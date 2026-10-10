using Application.Contractors;
using Application.Contractors.Authentication;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;
using Application.Services.Common;

namespace Application.Services;

public class TrialBalanceService : ITrialBalanceService
{
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<BankTransaction, long> _bankTransactionRepository;
    private readonly IRepository<Bank, int> _bankRepository;
    private readonly IBalanceCalculatorService _balanceCalculatorService;
    private readonly Guid _tenantId;

    public TrialBalanceService(
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<BankTransaction, long> bankTransactionRepository,
        IRepository<Bank, int> bankRepository,
        IBalanceCalculatorService balanceCalculatorService,
        ITenantProvider tenantProvider)
    {
        _transactionRepository = transactionRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _bankRepository = bankRepository;
        _balanceCalculatorService = balanceCalculatorService;
        _tenantId = tenantProvider.GetTenantId();
    }

    public Task<TrialBalanceSummaryResponse> GetTrialBalanceAsync(
        DateTime reportDate,
        CancellationToken cancellationToken)
    {
        return BuildAsync(ReportDateWindow.ForDay(reportDate), cancellationToken);
    }

    public Task<TrialBalanceSummaryResponse> GetTrialBalanceRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken)
    {
        return BuildAsync(ReportDateWindow.ForRange(fromDate, toDate), cancellationToken);
    }

    private async Task<TrialBalanceSummaryResponse> BuildAsync(
        ReportDateWindow window,
        CancellationToken cancellationToken)
    {
        // 1. Opening Balance at start of period (Capital / Equity -> CREDIT)
        var openingBalance = await _balanceCalculatorService.GetOpeningBalanceAsync(
            window.FromUtc, window.OpeningAnchorUtc, true, cancellationToken);

        // 2. Fetch nominal transactions for the period
        var transactions = await _transactionRepository.Query()
            .AsNoTracking()
            .Include(t => t.TransactionHead)
                .ThenInclude(h => h!.TransactionGroup)
            .Where(t => t.TenantId == _tenantId
                     && t.TransactionDate >= window.FromUtc && t.TransactionDate < window.ToUtc
                     && !t.IsDeleted && !t.IsArchived && t.PaymentMethod != PaymentMethods.CREDIT
                     && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE
                     && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
            .ToListAsync(cancellationToken);

        // Under standard accounting rules:
        //   Nominal Income (TransactionHeadTypes.DEBIT in FrostTrack, meaning money IN)  -> CREDIT column
        //   Nominal Expense (TransactionHeadTypes.CREDIT in FrostTrack, meaning money OUT) -> DEBIT column
        // Heads mapped to a Transaction Group are merged into one row (showing both Debit and Credit);
        // unmapped heads are grouped by their own name, split by type as before.
        var headRows = transactions
            .GroupBy(t =>
            {
                var head = t.TransactionHead!;
                var group = head.TransactionGroup;
                return group != null
                    ? new { Key = $"G:{group.Id}", Name = group.Name, IsGroup = true, IsIncome = false, group.SortOrder }
                    : new { Key = $"H:{head.Type}:{head.Name}", Name = head.Name, IsGroup = false, IsIncome = head.Type == TransactionHeadTypes.DEBIT, SortOrder = 0 };
            })
            .Select(g =>
            {
                var debit = g.Where(t => t.TransactionHead!.Type == TransactionHeadTypes.CREDIT).Sum(t => Math.Abs(t.NetAmount));
                var credit = g.Where(t => t.TransactionHead!.Type == TransactionHeadTypes.DEBIT).Sum(t => Math.Abs(t.NetAmount));
                return new
                {
                    g.Key.IsGroup,
                    g.Key.IsIncome,
                    g.Key.SortOrder,
                    Item = new TrialBalanceItemResponse
                    {
                        AccountName = g.Key.Name,
                        AccountType = g.Key.Name,
                        DebitAmount = debit,
                        CreditAmount = credit,
                        TransactionCount = g.Count(),
                        // Ungrouped rows keep their previous single-side balance; grouped rows show the net (Credit - Debit)
                        Balance = g.Key.IsGroup ? credit - debit : debit + credit
                    }
                };
            })
            .ToList();

        var groupItems = headRows
            .Where(r => r.IsGroup)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Item.AccountName)
            .Select(r => r.Item)
            .ToList();
        var expenseItems = headRows.Where(r => !r.IsGroup && !r.IsIncome).Select(r => r.Item).ToList();
        var revenueItems = headRows.Where(r => !r.IsGroup && r.IsIncome).Select(r => r.Item).ToList();

        // Direct bank receipts and payments during period (not contra, not auto-generated)
        var directBankTx = await _bankTransactionRepository.Query()
            .AsNoTracking()
            .Include(bt => bt.Bank)
            .Where(bt => bt.TenantId == _tenantId
                     && bt.TransactionDate >= window.FromUtc && bt.TransactionDate < window.ToUtc
                     && bt.IsActive && !bt.IsDeleted
                     && bt.SourceType != BankSourceTypes.CASH
                     && bt.SourceType != BankSourceTypes.CUSTOMER_PAYMENT
                     && bt.SourceType != BankSourceTypes.DELIVERY
                     && bt.TransactionId == null)
            .ToListAsync(cancellationToken);

        foreach (var dbt in directBankTx)
        {
            if (dbt.TransactionType == BankTransactionTypes.Deposit)
            {
                revenueItems.Add(new TrialBalanceItemResponse
                {
                    AccountName = $"{dbt.Bank?.BankName ?? "Bank"} - Direct Deposit",
                    AccountType = "Bank Receipt",
                    DebitAmount = 0,
                    CreditAmount = dbt.Amount,
                    TransactionCount = 1,
                    Balance = dbt.Amount
                });
            }
            else if (dbt.TransactionType == BankTransactionTypes.Withdraw)
            {
                expenseItems.Add(new TrialBalanceItemResponse
                {
                    AccountName = $"{dbt.Bank?.BankName ?? "Bank"} - Direct Payment",
                    AccountType = "Bank Payment",
                    DebitAmount = dbt.Amount,
                    CreditAmount = 0,
                    TransactionCount = 1,
                    Balance = dbt.Amount
                });
            }
        }

        // 3. Bank Accounts (Real Accounts / Assets -> DEBIT) as of end of period
        var banks = await _bankRepository.Query()
            .AsNoTracking()
            .Where(b => b.TenantId == _tenantId && b.IsActive && !b.IsDeleted)
            .ToListAsync(cancellationToken);

        var bankItems = new List<TrialBalanceItemResponse>();
        foreach (var bank in banks)
        {
            var bankTxSum = await _bankTransactionRepository.Query()
                .AsNoTracking()
                .Where(bt => bt.BankId == bank.Id && bt.IsActive && !bt.IsDeleted && bt.TransactionDate < window.ToUtc)
                .SumAsync(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount, cancellationToken);

            var endingBalance = bank.OpeningBalance + bankTxSum;
            if (endingBalance != 0)
            {
                bankItems.Add(new TrialBalanceItemResponse
                {
                    AccountName = $"{bank.BankName} - {bank.AccountNumber}",
                    AccountType = "Bank Transaction",
                    DebitAmount = endingBalance > 0 ? endingBalance : 0,
                    CreditAmount = endingBalance < 0 ? Math.Abs(endingBalance) : 0,
                    TransactionCount = 1,
                    Balance = endingBalance
                });
            }
        }

        // 4. Physical Cash in Hand as of end of period (Asset -> DEBIT)
        var cashInHandBalance = await _balanceCalculatorService.GetCashOpeningBalanceAsync(
            window.ToUtc, window.ClosingAnchorUtc, cancellationToken);

        var cashInHandItem = new TrialBalanceItemResponse
        {
            AccountName = "002-Cash in Hand",
            AccountType = "General",
            DebitAmount = cashInHandBalance > 0 ? cashInHandBalance : 0,
            CreditAmount = cashInHandBalance < 0 ? Math.Abs(cashInHandBalance) : 0,
            TransactionCount = 1,
            Balance = cashInHandBalance
        };

        // 5. Opening Balance / Capital Item (Capital/Equity -> CREDIT)
        var openingBalanceItem = new TrialBalanceItemResponse
        {
            AccountName = "Opening Balance",
            AccountType = "General",
            DebitAmount = openingBalance < 0 ? Math.Abs(openingBalance) : 0,
            CreditAmount = openingBalance >= 0 ? openingBalance : 0,
            TransactionCount = 1,
            Balance = openingBalance,
            SortOrder = 1
        };

        var allItems = new List<TrialBalanceItemResponse>();
        if (openingBalance != 0)
        {
            allItems.Add(openingBalanceItem);
        }

        allItems.AddRange(groupItems);
        allItems.AddRange(expenseItems);
        allItems.AddRange(revenueItems);
        allItems.AddRange(bankItems);
        allItems.Add(cashInHandItem);

        for (int i = 0; i < allItems.Count; i++)
        {
            allItems[i].SortOrder = i + 1;
        }

        var totalDebit = allItems.Sum(t => t.DebitAmount);
        var totalCredit = allItems.Sum(t => t.CreditAmount);
        var difference = Math.Round(totalDebit - totalCredit, 2);

        return new TrialBalanceSummaryResponse
        {
            ReportDate = window.ToDate,
            FromDate = window.FromDate,
            ToDate = window.ToDate,
            OpeningBalance = openingBalance,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            ClosingBalance = difference,
            TotalTransactions = allItems.Sum(i => i.TransactionCount),
            Items = allItems
        };
    }
}
