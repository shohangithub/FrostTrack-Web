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
    private readonly IRepository<Asset, int> _assetRepository;
    private readonly IBookingService _bookingService;
    private readonly IBalanceCalculatorService _balanceCalculatorService;
    private readonly Guid _tenantId;

    public BalanceSheetService(
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<BankTransaction, long> bankTransactionRepository,
        IRepository<Bank, int> bankRepository,
        IRepository<Asset, int> assetRepository,
        IBookingService bookingService,
        IBalanceCalculatorService balanceCalculatorService,
        ITenantProvider tenantProvider)
    {
        _transactionRepository = transactionRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _bankRepository = bankRepository;
        _assetRepository = assetRepository;
        _bookingService = bookingService;
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
            .Where(b => b.TenantId == _tenantId && b.IsActive && !b.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var bank in banks)
        {
            var txCount = await _bankTransactionRepository.Query()
                .Where(bt => bt.BankId == bank.Id && bt.TenantId == _tenantId && bt.IsActive && !bt.IsDeleted && bt.TransactionDate < toUtc)
                .CountAsync(cancellationToken);

            var bankTxSum = await _bankTransactionRepository.Query()
                .Where(bt => bt.BankId == bank.Id && bt.TenantId == _tenantId && bt.IsActive && !bt.IsDeleted && bt.TransactionDate < toUtc)
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
            else if (currentBankBalance < 0)
            {
                liabilities.Add(new BalanceSheetItemResponse
                {
                    AccountName = $"Bank Overdraft - {bank.BankName}",
                    AccountCategory = "Liability",
                    Amount = Math.Abs(currentBankBalance),
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

        // 3. ASSETS: Accounts Receivable (Customer Dues / বকেয়া বিল)
        try
        {
            var customerDues = await _bookingService.GetCustomerDueSummaryAsync(
                reportDate: reportDate,
                cancellationToken: cancellationToken);

            var totalAccountsReceivable = customerDues.Sum(c => c.TotalDue);
            if (totalAccountsReceivable > 0)
            {
                assets.Add(new BalanceSheetItemResponse
                {
                    AccountName = "Accounts Receivable (গ্রাহক বকেয়া)",
                    AccountCategory = "Asset",
                    Amount = totalAccountsReceivable,
                    TransactionCount = customerDues.Count(c => c.TotalDue > 0)
                });
            }
        }
        catch
        {
            // Fallback gracefully if customer dues report query encounters an edge case
        }

        // 4. ASSETS: Property, Plant & Equipment (Fixed Assets / স্থায়ী সম্পদ)
        var fixedAssets = await _assetRepository.Query()
            .Where(a => a.TenantId == _tenantId && a.IsActive && !a.IsDeleted && (a.PurchaseDate == null || a.PurchaseDate < toUtc))
            .ToListAsync(cancellationToken);

        var totalFixedAssets = fixedAssets.Sum(a => a.CurrentValue > 0 ? a.CurrentValue : a.PurchaseCost);
        if (totalFixedAssets > 0)
        {
            assets.Add(new BalanceSheetItemResponse
            {
                AccountName = "Property, Plant & Equipment (স্থায়ী সম্পদ)",
                AccountCategory = "Asset",
                Amount = totalFixedAssets,
                TransactionCount = fixedAssets.Count
            });
        }

        // 5. LIABILITIES: Accounts Payable & Credit liabilities up to the report date
        var accountsPayable = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t => t.TenantId == _tenantId && t.TransactionDate < toUtc && !t.IsDeleted && !t.IsArchived
                     && t.PaymentMethod == PaymentMethods.CREDIT
                     && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE
                     && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
            .SumAsync(t => t.NetAmount, cancellationToken);

        if (accountsPayable > 0)
        {
            liabilities.Add(new BalanceSheetItemResponse
            {
                AccountName = "Accounts Payable & Payables",
                AccountCategory = "Liability",
                Amount = accountsPayable,
                TransactionCount = 1
            });
        }

        // 6. EQUITY: Cumulative Net Retained Earnings (Revenue - Expenses up to toUtc)
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
                AccountName = "Retained Earnings (সঞ্চিত আয়)",
                AccountCategory = "Equity",
                Amount = retainedEarnings,
                TransactionCount = revenueExpenseCount
            });
        }

        var totalAssets = assets.Sum(a => a.Amount);
        var totalLiabilities = liabilities.Sum(l => l.Amount);
        var totalEquity = equity.Sum(e => e.Amount);

        // 7. Fundamental Accounting Equation: Assets = Liabilities + Equity
        // The net equity contribution financing the business assets is Owner's Capital:
        var ownersCapital = totalAssets - (totalLiabilities + totalEquity);
        if (Math.Abs(ownersCapital) > 0.001m)
        {
            equity.Add(new BalanceSheetItemResponse
            {
                AccountName = "Owner's Capital / Equity (মালিকের মূলধন)",
                AccountCategory = "Equity",
                Amount = ownersCapital,
                TransactionCount = 0
            });
            totalEquity += ownersCapital;
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

    public async Task<BalanceSheetComparativeResponse> GetComparativeBalanceSheetAsync(
        DateTime date1,
        DateTime date2,
        CancellationToken cancellationToken)
    {
        var sheet1 = await GetBalanceSheetAsync(date1, cancellationToken);
        var sheet2 = await GetBalanceSheetAsync(date2, cancellationToken);

        var assets = MergeCategory(sheet1.Assets, sheet2.Assets, "Asset");
        var liabilities = MergeCategory(sheet1.Liabilities, sheet2.Liabilities, "Liability");
        var equity = MergeCategory(sheet1.Equity, sheet2.Equity, "Equity");

        return new BalanceSheetComparativeResponse
        {
            Date1 = date1,
            Date2 = date2,
            Sheet1 = sheet1,
            Sheet2 = sheet2,
            Assets = assets,
            Liabilities = liabilities,
            Equity = equity
        };
    }

    private static List<ComparativeBalanceSheetItemResponse> MergeCategory(
        IEnumerable<BalanceSheetItemResponse> list1,
        IEnumerable<BalanceSheetItemResponse> list2,
        string category)
    {
        var dict1 = list1.ToDictionary(x => x.AccountName, x => x.Amount);
        var dict2 = list2.ToDictionary(x => x.AccountName, x => x.Amount);
        var allNames = dict1.Keys.Union(dict2.Keys).OrderBy(n => n);

        var result = new List<ComparativeBalanceSheetItemResponse>();
        foreach (var name in allNames)
        {
            dict1.TryGetValue(name, out var amt1);
            dict2.TryGetValue(name, out var amt2);
            var change = amt2 - amt1;
            var pct = amt1 != 0 ? Math.Round((change / Math.Abs(amt1)) * 100, 2) : 0;

            result.Add(new ComparativeBalanceSheetItemResponse
            {
                AccountName = name,
                AccountCategory = category,
                Date1Amount = amt1,
                Date2Amount = amt2,
                ChangeAmount = change,
                ChangePercentage = pct
            });
        }
        return result;
    }
}
