using Application.Contractors;
using Application.Contractors.Authentication;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;
using Application.Services.Common;

namespace Application.Services;

public class BalanceSheetService : IBalanceSheetService
{
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<BankTransaction, long> _bankTransactionRepository;
    private readonly IRepository<Bank, int> _bankRepository;
    private readonly IBalanceCalculatorService _balanceCalculatorService;
    private readonly Guid _tenantId;

    public BalanceSheetService(
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

    public async Task<BalanceSheetSummaryResponse> GetBalanceSheetAsync(
        DateTime reportDate,
        CancellationToken cancellationToken)
    {
        // Proper UTC time handling
        var fromLocal = reportDate.Date;
        var fromUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local)
            .ToUniversalTime();

        var toLocalExclusive = fromLocal.AddDays(1);
        var toUtc = DateTime.SpecifyKind(toLocalExclusive, DateTimeKind.Local)
            .ToUniversalTime();

        var dateWithUTCTime = reportDate.GetDateUtcTime();
        var toDate = toUtc > dateWithUTCTime ? toUtc : dateWithUTCTime;

        var openingBalance = await _balanceCalculatorService.GetOpeningBalanceAsync(fromUtc, toDate, true, cancellationToken);

        // Fetch all cash transactions up to and including the report date (excluding system transactions)
        var transactions = await _transactionRepository.Query().Include(t => t.TransactionHead)
            .Where(t => t.TenantId == _tenantId
                     && t.TransactionDate >= fromUtc && t.TransactionDate < toUtc
                     && !t.IsDeleted
                     && !t.IsArchived
                     && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE
                     && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE).ToListAsync(cancellationToken);

        // Fetch all bank transactions up to the date
        var bankTransactions = await _bankTransactionRepository.Query()
            .Include(bt => bt.Bank)
            .Where(bt => bt.TenantId == _tenantId
                      && bt.TransactionDate >= fromUtc && bt.TransactionDate < toUtc
                      && bt.IsActive
                      && !bt.IsArchived).ToListAsync(cancellationToken);

        // Fetch bank accounts for current balances
        // var bankQuery = _bankRepository.Query()
        //     .Where(b => b.TenantId == _tenantId && b.IsActive);

        // var banks = await bankQuery.ToListAsync(cancellationToken);

        var assets = new List<BalanceSheetItemResponse>();
        var liabilities = new List<BalanceSheetItemResponse>();
        var equity = new List<BalanceSheetItemResponse>();

        // ASSETS: Calculate cash in hand from transactions
        //  var cashInflow = transactions.Where(t => t.TransactionHead?.Type == TransactionHeadTypes.CREDIT && !t.IsArchived).Sum(t => t.NetAmount);
        var transactionAmount = transactions.Sum(t => t.TransactionHead?.Type == TransactionHeadTypes.CREDIT ? t.NetAmount : -t.NetAmount);
        var bankTransactionAmount = bankTransactions.Sum(bt => bt.TransactionType == BankTransactionTypes.Deposit ? -bt.Amount : bt.Amount);

        var cashInHand = openingBalance + transactionAmount + bankTransactionAmount;

        if (cashInHand != 0)
        {
            assets.Add(new BalanceSheetItemResponse
            {
                AccountName = "Cash in Hand",
                AccountCategory = "Asset",
                Amount = cashInHand,
                TransactionCount = transactions.Count
            });
        }

        // ASSETS: Add bank balances

        var groupBankTransactions = bankTransactions
             .GroupBy(bt => new { bt.BankId, bt.Bank.BankName })
             .Select(g => new BalanceSheetItemResponse
             {
                 AccountName = $"Bank - {g.Key.BankName}",
                 AccountCategory = "Asset",
                 Amount = g.Sum(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount),
                 TransactionCount = g.Count()
             }).ToList();

        assets.AddRange(groupBankTransactions.Where(b => b.Amount > 0));


        // LIABILITIES: Calculate accounts payable (unpaid bills)
        var accountsPayable = transactions
            .Where(t => t.TransactionHead?.UsageFor == UsageFor.BILL_COLLECTION && t.TransactionHead?.Type == TransactionHeadTypes.DEBIT)
            .Sum(t => t.NetAmount);

        if (accountsPayable > 0)
        {
            liabilities.Add(new BalanceSheetItemResponse
            {
                AccountName = "Accounts Payable",
                AccountCategory = "Liability",
                Amount = accountsPayable,
                TransactionCount = transactions.Count(t => t.TransactionHead?.UsageFor == UsageFor.BILL_COLLECTION && t.TransactionHead?.Type == TransactionHeadTypes.DEBIT)
            });
        }

        // EQUITY: Calculate from bill collections and revenue
        var revenue = transactions
            .Where(t => t.TransactionHead?.UsageFor == UsageFor.BILL_COLLECTION && t.TransactionHead?.Type == TransactionHeadTypes.CREDIT)
            .Sum(t => t.NetAmount);

        var expenses = transactions
            .Where(t => (t.TransactionHead?.UsageFor == UsageFor.TRANSACTION || t.TransactionHead?.UsageFor == UsageFor.SALARY)
                     && t.TransactionHead?.Type == TransactionHeadTypes.DEBIT)
            .Sum(t => t.NetAmount);

        var retainedEarnings = revenue - expenses;

        if (retainedEarnings != 0)
        {
            equity.Add(new BalanceSheetItemResponse
            {
                AccountName = "Retained Earnings",
                AccountCategory = "Equity",
                Amount = retainedEarnings,
                TransactionCount = transactions.Count(t =>
                    t.TransactionHead?.UsageFor == UsageFor.BILL_COLLECTION ||
                    t.TransactionHead?.UsageFor == UsageFor.TRANSACTION ||
                    t.TransactionHead?.UsageFor == UsageFor.SALARY)
            });
        }

        var totalAssets = assets.Sum(a => a.Amount);
        var totalLiabilities = liabilities.Sum(l => l.Amount);
        var totalEquity = equity.Sum(e => e.Amount);

        // Balance the equation: Assets = Liabilities + Equity
        var difference = totalAssets - (totalLiabilities + totalEquity);
        if (Math.Abs(difference) > 0.01m) // Add balancing item if needed
        {
            if (difference > 0)
            {
                equity.Add(new BalanceSheetItemResponse
                {
                    AccountName = "Owner's Capital",
                    AccountCategory = "Equity",
                    Amount = difference,
                    TransactionCount = 0
                });
                totalEquity += difference;
            }
            else
            {
                liabilities.Add(new BalanceSheetItemResponse
                {
                    AccountName = "Other Liabilities",
                    AccountCategory = "Liability",
                    Amount = Math.Abs(difference),
                    TransactionCount = 0
                });
                totalLiabilities += Math.Abs(difference);
            }
        }

        // Calculate closing balance
        var closingCashInflow = transactions
            .Where(t => t.TransactionHead?.Type == TransactionHeadTypes.CREDIT && !t.IsArchived)
            .Sum(t => t.NetAmount);
        var closingCashOutflow = transactions
            .Where(t => t.TransactionHead?.Type == TransactionHeadTypes.DEBIT && !t.IsArchived)
            .Sum(t => t.NetAmount);

        // Add bank balances to closing balance
        var bankBalance = bankTransactions.Sum(bt => bt.TransactionType == BankTransactionTypes.Deposit ? -bt.Amount : bt.Amount);
        var closingBalance = openingBalance + closingCashInflow - closingCashOutflow + bankBalance;

        return new BalanceSheetSummaryResponse
        {
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            TotalEquity = totalEquity,
            NetWorth = totalAssets - totalLiabilities,
            ReportDate = reportDate,
            TotalTransactions = transactions.Count + bankTransactions.Count,
            OpeningBalance = openingBalance,
            ClosingBalance = closingBalance,
            Assets = assets.OrderBy(a => a.AccountName).ToList(),
            Liabilities = liabilities.OrderBy(l => l.AccountName).ToList(),
            Equity = equity.OrderBy(e => e.AccountName).ToList()
        };
    }
}
