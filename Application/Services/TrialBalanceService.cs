using Domain.Entitites;
using Microsoft.EntityFrameworkCore;
using Application.Services.Common;

namespace Application.Services;

public class TrialBalanceService : ITrialBalanceService
{
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<BankTransaction, long> _bankTransactionRepository;
    private readonly IBalanceCalculatorService _balanceCalculatorService;
    private readonly Guid _tenantId;

    public TrialBalanceService(
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<BankTransaction, long> bankTransactionRepository,
        IBalanceCalculatorService balanceCalculatorService,
        ITenantProvider tenantProvider)
    {
        _transactionRepository = transactionRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _balanceCalculatorService = balanceCalculatorService;
        _tenantId = tenantProvider.GetTenantId();
    }

    public async Task<TrialBalanceSummaryResponse> GetTrialBalanceAsync(
        DateTime reportDate,
        CancellationToken cancellationToken)
    {
        // Get opening balance (all transactions before reportDate)
        var fromLocal = reportDate.Date;
        var fromUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local)
            .ToUniversalTime();

        var toLocalExclusive = fromLocal.AddDays(1);
        var toUtc = DateTime.SpecifyKind(toLocalExclusive, DateTimeKind.Local)
            .ToUniversalTime();

        var dateWithUTCTime = reportDate.GetDateUtcTime();
        var toDate = toUtc > dateWithUTCTime ? toUtc : dateWithUTCTime;

        var openingBalance = await _balanceCalculatorService.GetOpeningBalanceAsync(fromUtc, toDate, true, cancellationToken);

        // Fetch cash transactions for the report date
        var transactionQuery = _transactionRepository.Query().Include(t => t.TransactionHead)
            .Where(t => t.TenantId == _tenantId
                     && t.TransactionDate >= fromUtc && t.TransactionDate < toUtc
                     && !t.IsDeleted && !t.IsArchived
                     && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE);

        var transactions = await transactionQuery.ToListAsync(cancellationToken);

        // Fetch bank transactions for the report date
        var bankTransactionQuery = _bankTransactionRepository.Query()
            .Include(bt => bt.Bank)
            .Where(bt => bt.TenantId == _tenantId
                      && bt.TransactionDate >= fromUtc && bt.TransactionDate < toUtc
                      && bt.IsActive);

        var bankTransactions = await bankTransactionQuery.ToListAsync(cancellationToken);

        // Group cash transactions by type and calculate debits/credits
        var groupedTransactions = transactions
            .GroupBy(t => new { t.TransactionHead!.Name })
            .Select(g => new TrialBalanceItemResponse
            {
                AccountName = g.Key.Name,
                AccountType = g.Key.Name,
                DebitAmount = g.Where(t => t.TransactionHead!.Type == TransactionHeadTypes.CREDIT) // Money IN = Debit
                              .Sum(t => t.NetAmount),
                CreditAmount = g.Where(t => t.TransactionHead!.Type == TransactionHeadTypes.DEBIT) // Money OUT = Credit
                               .Sum(t => t.NetAmount),
                TransactionCount = g.Count(),
                Balance = g.Where(t => t.TransactionHead!.Type == TransactionHeadTypes.CREDIT)
                           .Sum(t => t.NetAmount) -
                          g.Where(t => t.TransactionHead!.Type == TransactionHeadTypes.DEBIT)
                           .Sum(t => t.NetAmount)
            })
            .ToList();

        // Group bank transactions by bank and transaction type
        var groupedBankTransactions = bankTransactions
            .GroupBy(bt => new { bt.Bank.BankName, bt.TransactionType })
            .Select(g => new TrialBalanceItemResponse
            {
                AccountName = $"{g.Key.BankName} - {g.Key.TransactionType}",
                AccountType = "Bank Transaction",
                // Bank Deposit = money IN (DEBIT side); Withdrawal = money OUT (CREDIT side)
                DebitAmount = g.Where(bt => bt.TransactionType == BankTransactionTypes.Deposit)
                              .Sum(bt => bt.Amount),
                CreditAmount = g.Where(bt => bt.TransactionType == BankTransactionTypes.Withdraw)
                               .Sum(bt => bt.Amount),
                TransactionCount = g.Count(),
                Balance = g.Where(bt => bt.TransactionType == BankTransactionTypes.Deposit)
                           .Sum(bt => bt.Amount) -
                          g.Where(bt => bt.TransactionType == BankTransactionTypes.Withdraw)
                           .Sum(bt => bt.Amount)
            })
            .ToList();



        // Combine both lists
        var mergeItems = groupedTransactions.Concat(groupedBankTransactions).Select(
            (item, index) =>
            {
                item.SortOrder = index + 2; // Start from 2 to leave space for Cash in Hand
                return item;
            }
        ).ToList();

        var totalDebit = mergeItems.Sum(t => t.DebitAmount);
        var totalCredit = mergeItems.Sum(t => t.CreditAmount);
        var closingBalance = openingBalance + totalCredit - totalDebit;



        var cashinHand = new List<TrialBalanceItemResponse> {
             new TrialBalanceItemResponse
            {
                AccountName = $"Opening Balance",
                AccountType = "General",
                DebitAmount = 0,
                CreditAmount = openingBalance > 0 ? openingBalance : 0,
                TransactionCount = 1,
                Balance = openingBalance > 0 ? openingBalance : 0,
                SortOrder = 1
            },
            new TrialBalanceItemResponse
            {
                AccountName = $"002-Cash in Hand",
                AccountType = "General",
                DebitAmount = closingBalance > 0 ? closingBalance : 0,
                CreditAmount = 0,
                TransactionCount = 1,
                Balance = closingBalance > 0 ? closingBalance : 0,
                SortOrder = mergeItems.Count + 2
            }
        };

        var allItems = mergeItems.Concat(cashinHand).OrderBy(item => item.SortOrder)
                    .ToList();

        var _totalDebit = allItems.Sum(t => t.DebitAmount);
        var _totalCredit = allItems.Sum(t => t.CreditAmount);
        // var _totalTransactionCount = transactions.Count + bankTransactions.Count;
        var _closingBalance = openingBalance + _totalCredit - _totalDebit;

        return new TrialBalanceSummaryResponse
        {
            ReportDate = reportDate,
            OpeningBalance = openingBalance,
            TotalDebit = _totalDebit,
            TotalCredit = _totalCredit,
            ClosingBalance = _closingBalance,
            //TotalTransactions = _totalTransactionCount,
            Items = allItems
        };
    }
}
