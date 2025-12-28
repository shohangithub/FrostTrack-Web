using Application.Contractors;
using Application.Contractors.Authentication;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class BalanceSheetService : IBalanceSheetService
{
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<BankTransaction, long> _bankTransactionRepository;
    private readonly IRepository<Bank, int> _bankRepository;
    private readonly Guid _tenantId;

    public BalanceSheetService(
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<BankTransaction, long> bankTransactionRepository,
        IRepository<Bank, int> bankRepository,
        ITenantProvider tenantProvider)
    {
        _transactionRepository = transactionRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _bankRepository = bankRepository;
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

        // Get last opening balance
        var lastOpeningBalance = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t =>
                t.TenantId == _tenantId &&
                !t.IsArchived &&
                t.TransactionHead!.UsageFor == UsageFor.OPENING_BALANCE &&
                t.TransactionDate < dateWithUTCTime)
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new
            {
                t.TransactionDate,
                t.NetAmount
            })
            .FirstOrDefaultAsync(cancellationToken);

        var openingDate = lastOpeningBalance?.TransactionDate ?? dateWithUTCTime;

        // Calculate opening balance from cash
        var previousCashAmount = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t =>
                t.TenantId == _tenantId &&
                !t.IsArchived &&
                t.TransactionDate >= openingDate &&
                t.TransactionDate < fromUtc &&
                t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE &&
                t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
            .SumAsync(t => t.NetAmount, cancellationToken);

        // Calculate opening balance from bank
        var previousBankAmount = await _bankTransactionRepository.Query()
            .Where(bt =>
                bt.TenantId == _tenantId &&
                bt.IsActive &&
                bt.TransactionDate >= openingDate &&
                bt.TransactionDate < fromUtc)
            .SumAsync(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount, cancellationToken);

        var openingBalance = (lastOpeningBalance?.NetAmount ?? 0) + previousCashAmount + previousBankAmount;

        // Fetch all cash transactions up to and including the report date (excluding system transactions)
        var transactionQuery = _transactionRepository.Query().Include(t => t.TransactionHead)
            .Where(t => t.TenantId == _tenantId
                     && t.TransactionDate < toUtc
                     && !t.IsDeleted
                     && !t.IsArchived
                     && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE
                     && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE);

        var transactions = await transactionQuery.ToListAsync(cancellationToken);

        // Fetch all bank transactions up to the date
        var bankTransactionQuery = _bankTransactionRepository.Query()
            .Include(bt => bt.Bank)
            .Where(bt => bt.TenantId == _tenantId
                      && bt.TransactionDate < toUtc
                      && bt.IsActive);

        var bankTransactions = await bankTransactionQuery.ToListAsync(cancellationToken);

        // Fetch bank accounts for current balances
        var bankQuery = _bankRepository.Query()
            .Where(b => b.TenantId == _tenantId && b.IsActive);

        var banks = await bankQuery.ToListAsync(cancellationToken);

        var assets = new List<BalanceSheetItemResponse>();
        var liabilities = new List<BalanceSheetItemResponse>();
        var equity = new List<BalanceSheetItemResponse>();

        // ASSETS: Calculate cash in hand from transactions
        var cashInflow = transactions.Where(t => t.TransactionHead?.Type == TransactionHeadTypes.CREDIT && !t.IsArchived).Sum(t => t.NetAmount);
        var cashOutflow = transactions.Where(t => t.TransactionHead?.Type == TransactionHeadTypes.DEBIT && !t.IsArchived).Sum(t => t.NetAmount);
        var cashInHand = openingBalance + cashInflow + cashOutflow;

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
        foreach (var bank in banks)
        {
            if (bank.CurrentBalance > 0)
            {
                var bankTxCount = bankTransactions.Count(bt => bt.BankId == bank.Id);
                assets.Add(new BalanceSheetItemResponse
                {
                    AccountName = $"Bank - {bank.BankName}",
                    AccountCategory = "Asset",
                    Amount = bank.CurrentBalance,
                    TransactionCount = bankTxCount
                });
            }
        }

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

        var retainedEarnings = revenue + expenses;

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
        var bankBalance = bankTransactions.Sum(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount);
        var closingBalance = openingBalance + closingCashInflow + closingCashOutflow + bankBalance;

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
