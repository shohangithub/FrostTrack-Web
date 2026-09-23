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

        var assets = new List<BalanceSheetItemResponse>();
        var liabilities = new List<BalanceSheetItemResponse>();
        var equity = new List<BalanceSheetItemResponse>();

        // 1. ASSETS: Bank balances as of the report date (up to toUtc)
        var banks = await _bankRepository.Query()
            .Where(b => b.TenantId == _tenantId && b.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var bank in banks)
        {
            var txCount = await _bankTransactionRepository.Query()
                .Where(bt => bt.BankId == bank.Id && bt.IsActive && !bt.IsDeleted && bt.TransactionDate < toUtc)
                .CountAsync(cancellationToken);

            var bankTxSum = await _bankTransactionRepository.Query()
                .Where(bt => bt.BankId == bank.Id && bt.IsActive && !bt.IsDeleted && bt.TransactionDate < toUtc)
                .SumAsync(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount, cancellationToken);

            var currentBankBalance = bank.OpeningBalance + bankTxSum;
            if (currentBankBalance > 0)
            {
                assets.Add(new BalanceSheetItemResponse
                {
                    AccountName = $"Bank - {bank.BankName}",
                    AccountCategory = "Asset",
                    Amount = currentBankBalance,
                    TransactionCount = txCount
                });
            }
        }

        // 2. ASSETS: Physical Cash in Hand as of report date (up to toUtc)
        var cashInHand = await _balanceCalculatorService.GetCashOpeningBalanceAsync(toUtc, toDate, cancellationToken);
        if (cashInHand != 0)
        {
            var cashTxCount = await _transactionRepository.Query()
                .Where(t => t.TenantId == _tenantId && t.TransactionDate < toUtc && !t.IsDeleted && !t.IsArchived
                         && t.PaymentMethod == PaymentMethods.CASH
                         && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE
                         && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
                .CountAsync(cancellationToken);

            assets.Add(new BalanceSheetItemResponse
            {
                AccountName = "Cash in Hand",
                AccountCategory = "Asset",
                Amount = cashInHand,
                TransactionCount = cashTxCount
            });
        }

        // 3. LIABILITIES: Accounts payable up to the report date
        var accountsPayable = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t => t.TenantId == _tenantId && t.TransactionDate < toUtc && !t.IsDeleted && !t.IsArchived
                     && t.TransactionHead!.UsageFor == UsageFor.CUSTOMER_PAYMENT && t.TransactionHead!.Type == TransactionHeadTypes.CREDIT)
            .SumAsync(t => t.NetAmount, cancellationToken);

        if (accountsPayable > 0)
        {
            liabilities.Add(new BalanceSheetItemResponse
            {
                AccountName = "Accounts Payable",
                AccountCategory = "Liability",
                Amount = accountsPayable,
                TransactionCount = 1
            });
        }

        // 4. EQUITY: Cumulative Net Retained Earnings (Revenue - Expenses up to toUtc)
        var cumulativeRevenue = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t => t.TenantId == _tenantId && t.TransactionDate < toUtc && !t.IsDeleted && !t.IsArchived
                     && t.TransactionHead!.Type == TransactionHeadTypes.DEBIT
                     && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE
                     && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
            .SumAsync(t => t.NetAmount, cancellationToken);

        var cumulativeExpenses = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t => t.TenantId == _tenantId && t.TransactionDate < toUtc && !t.IsDeleted && !t.IsArchived
                     && t.TransactionHead!.Type == TransactionHeadTypes.CREDIT
                     && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE
                     && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
            .SumAsync(t => t.NetAmount, cancellationToken);

        var retainedEarnings = cumulativeRevenue - cumulativeExpenses;
        var revenueExpenseCount = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t => t.TenantId == _tenantId && t.TransactionDate < toUtc && !t.IsDeleted && !t.IsArchived
                     && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE
                     && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
            .CountAsync(cancellationToken);

        if (retainedEarnings != 0)
        {
            equity.Add(new BalanceSheetItemResponse
            {
                AccountName = "Retained Earnings",
                AccountCategory = "Equity",
                Amount = retainedEarnings,
                TransactionCount = revenueExpenseCount
            });
        }

        var totalAssets = assets.Sum(a => a.Amount);
        var totalLiabilities = liabilities.Sum(l => l.Amount);
        var totalEquity = equity.Sum(e => e.Amount);

        // 5. Balance the equation: Assets = Liabilities + Equity
        // The difference between Net Assets and Retained Earnings is Owner's Capital
        var difference = totalAssets - (totalLiabilities + totalEquity);
        if (Math.Abs(difference) > 0.01m)
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

        return new BalanceSheetSummaryResponse
        {
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            TotalEquity = totalEquity,
            NetWorth = totalAssets - totalLiabilities,
            ReportDate = reportDate,
            TotalTransactions = assets.Sum(a => a.TransactionCount),
            OpeningBalance = openingBalance,
            ClosingBalance = totalAssets,
            Assets = assets.OrderBy(a => a.AccountName).ToList(),
            Liabilities = liabilities.OrderBy(l => l.AccountName).ToList(),
            Equity = equity.OrderBy(e => e.AccountName).ToList()
        };
    }
}
