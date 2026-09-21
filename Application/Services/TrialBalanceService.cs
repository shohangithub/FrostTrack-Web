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

    public async Task<TrialBalanceSummaryResponse> GetTrialBalanceAsync(
        DateTime reportDate,
        CancellationToken cancellationToken)
    {
        var fromLocal = reportDate.Date;
        var fromUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local)
            .ToUniversalTime();

        var toLocalExclusive = fromLocal.AddDays(1);
        var toUtc = DateTime.SpecifyKind(toLocalExclusive, DateTimeKind.Local)
            .ToUniversalTime();

        var dateWithUTCTime = reportDate.GetDateUtcTime();
        var toDate = toUtc > dateWithUTCTime ? toUtc : dateWithUTCTime;

        // 1. Opening Balance at start of report date (Capital / Equity -> CREDIT)
        var openingBalance = await _balanceCalculatorService.GetOpeningBalanceAsync(fromUtc, toDate, true, cancellationToken);

        // 2. Fetch nominal transactions for the report date
        var transactions = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t => t.TenantId == _tenantId
                     && t.TransactionDate >= fromUtc && t.TransactionDate < toUtc
                     && !t.IsDeleted && !t.IsArchived && t.PaymentMethod != PaymentMethods.CREDIT
                     && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE
                     && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
            .ToListAsync(cancellationToken);

        // Under standard accounting rules:
        // Nominal Income / Revenue (TransactionHeadTypes.DEBIT in FrostTrack, meaning money IN) -> CREDIT
        var revenueItems = transactions
            .Where(t => t.TransactionHead!.Type == TransactionHeadTypes.DEBIT)
            .GroupBy(t => new { t.TransactionHead!.Name })
            .Select(g => new TrialBalanceItemResponse
            {
                AccountName = g.Key.Name,
                AccountType = g.Key.Name,
                DebitAmount = 0,
                CreditAmount = g.Sum(t => Math.Abs(t.NetAmount)),
                TransactionCount = g.Count(),
                Balance = g.Sum(t => Math.Abs(t.NetAmount))
            })
            .ToList();

        // Nominal Expense (TransactionHeadTypes.CREDIT in FrostTrack, meaning money OUT) -> DEBIT
        var expenseItems = transactions
            .Where(t => t.TransactionHead!.Type == TransactionHeadTypes.CREDIT)
            .GroupBy(t => new { t.TransactionHead!.Name })
            .Select(g => new TrialBalanceItemResponse
            {
                AccountName = g.Key.Name,
                AccountType = g.Key.Name,
                DebitAmount = g.Sum(t => Math.Abs(t.NetAmount)),
                CreditAmount = 0,
                TransactionCount = g.Count(),
                Balance = g.Sum(t => Math.Abs(t.NetAmount))
            })
            .ToList();

        // 3. Bank Accounts (Real Accounts / Assets -> DEBIT)
        var banks = await _bankRepository.Query()
            .Where(b => b.TenantId == _tenantId && b.IsActive)
            .ToListAsync(cancellationToken);

        var bankItems = new List<TrialBalanceItemResponse>();
        foreach (var bank in banks)
        {
            var bankTxSum = await _bankTransactionRepository.Query()
                .Where(bt => bt.BankId == bank.Id && bt.IsActive && !bt.IsDeleted && bt.TransactionDate < toUtc)
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

        // 4. Physical Cash in Hand as of end-of-day (Asset -> DEBIT)
        var cashInHandBalance = await _balanceCalculatorService.GetCashOpeningBalanceAsync(toUtc, toDate, cancellationToken);

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

        // Assemble all items in proper presentation order:
        // Capital -> Revenues -> Expenses -> Bank Accounts -> Cash in Hand
        var allItems = new List<TrialBalanceItemResponse>();
        if (openingBalance != 0)
        {
            allItems.Add(openingBalanceItem);
        }

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
            ReportDate = reportDate,
            OpeningBalance = openingBalance,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            ClosingBalance = difference,
            Items = allItems
        };
    }
}
