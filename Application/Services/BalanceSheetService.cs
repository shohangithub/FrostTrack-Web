using Application.Contractors;
using Application.Contractors.Authentication;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;
using Application.Services.Common;

namespace Application.Services;

/// <summary>
/// Balance Sheet (Statement of Financial Position) as at a date.
/// The position on Company.BalanceSheetStartDate comes from BalanceSheetEntries (OPENING); after it:
/// cash, bank, customer balances and fixed assets are calculated as at the date, head-based lines
/// (loans, deposits, capital) and profit are summed from the day after the start date.
/// Rent income is accrual based (charge ledger); customer payments only settle receivables.
/// </summary>
public class BalanceSheetService : IBalanceSheetService
{
    private const string ASSET = "Asset";
    private const string LIABILITY = "Liability";
    private const string EQUITY = "Equity";

    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<BankTransaction, long> _bankTransactionRepository;
    private readonly IRepository<Bank, int> _bankRepository;
    private readonly IRepository<Asset, int> _assetRepository;
    private readonly IRepository<Customer, int> _customerRepository;
    private readonly IRepository<BalanceSheetEntry, Guid> _entryRepository;
    private readonly IRepository<RecurringChargeEntry, Guid> _chargeRepository;
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly IBookingLedgerService _ledgerService;
    private readonly IBalanceCalculatorService _balanceCalculatorService;
    private readonly Guid _tenantId;

    public BalanceSheetService(
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<BankTransaction, long> bankTransactionRepository,
        IRepository<Bank, int> bankRepository,
        IRepository<Asset, int> assetRepository,
        IRepository<Customer, int> customerRepository,
        IRepository<BalanceSheetEntry, Guid> entryRepository,
        IRepository<RecurringChargeEntry, Guid> chargeRepository,
        IRepository<Booking, Guid> bookingRepository,
        IRepository<Company, int> companyRepository,
        IBookingLedgerService ledgerService,
        IBalanceCalculatorService balanceCalculatorService,
        ITenantProvider tenantProvider)
    {
        _transactionRepository = transactionRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _bankRepository = bankRepository;
        _assetRepository = assetRepository;
        _customerRepository = customerRepository;
        _entryRepository = entryRepository;
        _chargeRepository = chargeRepository;
        _bookingRepository = bookingRepository;
        _companyRepository = companyRepository;
        _ledgerService = ledgerService;
        _balanceCalculatorService = balanceCalculatorService;
        _tenantId = tenantProvider.GetTenantId();
    }

    public async Task<BalanceSheetSummaryResponse> GetBalanceSheetAsync(
        DateTime reportDate,
        CancellationToken cancellationToken)
    {
        var day = reportDate.Date;
        var response = new BalanceSheetSummaryResponse { ReportDate = reportDate };

        var settings = await _companyRepository.Query()
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .Select(c => new { c.BalanceSheetStartDate, c.FiscalYearStartMonth })
            .FirstOrDefaultAsync(cancellationToken);

        if (settings?.BalanceSheetStartDate is null)
        {
            response.Message = "Set the balance sheet start date and opening balances first (Accounts → Opening Balances).";
            return response;
        }

        var startDay = settings.BalanceSheetStartDate.Value.Date;
        var fyMonth = settings.FiscalYearStartMonth is >= 1 and <= 12 ? settings.FiscalYearStartMonth : 7;
        response.IsConfigured = true;
        response.StartDate = startDay;

        if (day < startDay)
        {
            response.Message = $"The report date is before the balance sheet start date ({startDay:dd-MMM-yyyy}).";
            return response;
        }

        // Windows (local calendar days → UTC). Profit before the current fiscal year goes to retained earnings b/f.
        var fyStartDay = FiscalYear.StartOf(day, fyMonth);
        var periodStartDay = startDay.AddDays(1);
        var currentYearFromDay = fyStartDay > periodStartDay ? fyStartDay : periodStartDay;
        response.FiscalYearStart = currentYearFromDay;

        var periodStartUtc = ReportDateWindow.DayStartUtc(periodStartDay);
        var currentYearFromUtc = ReportDateWindow.DayStartUtc(currentYearFromDay);
        var toUtc = ReportDateWindow.DayStartUtc(day.AddDays(1));
        var anchorUtc = ReportDateWindow.OpeningAnchorFor(day);

        var profitBefore = new ProfitBuckets();
        var profitCurrent = new ProfitBuckets();
        ProfitBuckets Bucket(bool currentYear) => currentYear ? profitCurrent : profitBefore;

        var assets = new List<BalanceSheetItemResponse>();
        var liabilities = new List<BalanceSheetItemResponse>();
        var equity = new List<BalanceSheetItemResponse>();

        // ── 1. Fixed assets (written down value) and depreciation ─────────────
        var fixedAssets = await _assetRepository.Query()
            .AsNoTracking()
            .Where(a => a.TenantId == _tenantId && a.IsActive && !a.IsDeleted && (a.PurchaseDate == null || a.PurchaseDate < toUtc))
            .ToListAsync(cancellationToken);

        decimal totalWdv = 0m;
        foreach (var asset in fixedAssets)
        {
            var atStart = DepreciationCalculator.GetWdv(asset, startDay, startDay, fyMonth);
            var beforeCurrentYear = DepreciationCalculator.GetWdv(asset, currentYearFromDay.AddDays(-1), startDay, fyMonth);
            var atDay = DepreciationCalculator.GetWdv(asset, day, startDay, fyMonth);
            profitBefore.Depreciation += atStart - beforeCurrentYear;
            profitCurrent.Depreciation += beforeCurrentYear - atDay;
            totalWdv += atDay;
        }

        // ── 2. Cash and bank ──────────────────────────────────────────────────
        var cashInHand = await _balanceCalculatorService.GetCashOpeningBalanceAsync(toUtc, anchorUtc, cancellationToken);

        var banks = await _bankRepository.Query()
            .AsNoTracking()
            .Where(b => b.TenantId == _tenantId && b.IsActive && !b.IsDeleted)
            .Select(b => new { b.Id, b.BankName, b.OpeningBalance })
            .ToListAsync(cancellationToken);

        var bankMovements = await _bankTransactionRepository.Query()
            .AsNoTracking()
            .Where(bt => bt.TenantId == _tenantId && bt.IsActive && !bt.IsDeleted && bt.TransactionDate < toUtc)
            .GroupBy(bt => bt.BankId)
            .Select(g => new
            {
                BankId = g.Key,
                Count = g.Count(),
                Net = g.Sum(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount)
            })
            .ToDictionaryAsync(x => x.BankId, cancellationToken);

        // ── 3. Customers: receivables (positive balances) and advances (negative) ─
        // Same as the Customer Due report (opening + charges − payments − discounts per customer), but cut off
        // at the end of the report day: rent of the next month, posted at 00:00 on its first day, is not due yet.
        var activeBookingIds = await _bookingRepository.Query()
            .Where(b => b.TenantId == _tenantId && !b.IsDeleted && !b.IsArchived)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
        await _ledgerService.SyncBookingsAsync(activeBookingIds, cancellationToken: cancellationToken);

        var customerBalances = await _customerRepository.Query()
            .AsNoTracking()
            .Where(c => c.TenantId == _tenantId && !c.IsDeleted && c.OpeningBalance != 0)
            .ToDictionaryAsync(c => c.Id, c => c.OpeningBalance, cancellationToken);
        void AddToCustomer(int? customerId, decimal amount)
        {
            if (customerId is null) return;
            customerBalances[customerId.Value] = customerBalances.GetValueOrDefault(customerId.Value) + amount;
        }

        var chargesByCustomer = await _chargeRepository.Query()
            .AsNoTracking()
            .Where(e => e.ChargeType != ChargeTypes.Legacy && e.EntryDate < toUtc && !e.Booking!.IsDeleted && !e.Booking.IsArchived)
            .GroupBy(e => e.Booking!.CustomerId)
            .Select(g => new { CustomerId = g.Key, Amount = g.Sum(e => e.Amount) })
            .ToListAsync(cancellationToken);
        foreach (var c in chargesByCustomer) AddToCustomer(c.CustomerId, c.Amount);

        var settlementsByCustomer = await _transactionRepository.Query()
            .AsNoTracking()
            .Where(t => t.TenantId == _tenantId && !t.IsDeleted && !t.IsArchived && t.TransactionDate < toUtc
                        && ((t.TransactionHead!.UsageFor == UsageFor.CUSTOMER_PAYMENT && t.TransactionHead.Type == TransactionHeadTypes.DEBIT)
                            || t.TransactionHead.UsageFor == UsageFor.BILL_DISCOUNT))
            .GroupBy(t => t.CustomerId ?? (int?)t.Booking!.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Amount = g.Sum(t => t.TransactionHead!.UsageFor == UsageFor.BILL_DISCOUNT
                    ? (t.Amount > 0 ? t.Amount : t.DiscountAmount)
                    : t.Amount)
            })
            .ToListAsync(cancellationToken);
        foreach (var s in settlementsByCustomer) AddToCustomer(s.CustomerId, -s.Amount);

        var receivables = customerBalances.Values.Where(b => b > 0.005m).ToList();
        var advances = customerBalances.Values.Where(b => b < -0.005m).ToList();

        // ── 4. Rent & labour income (charge ledger, accrual) ──────────────────
        var chargesAtStart = await _ledgerService.GetTotalChargesBeforeAsync(periodStartUtc, cancellationToken);
        var chargesBeforeCurrentYear = currentYearFromDay > periodStartDay
            ? await _ledgerService.GetTotalChargesBeforeAsync(currentYearFromUtc, cancellationToken)
            : chargesAtStart;
        var chargesAtDay = await _ledgerService.GetTotalChargesBeforeAsync(toUtc, cancellationToken);
        profitBefore.Charges = chargesBeforeCurrentYear - chargesAtStart;
        profitCurrent.Charges = chargesAtDay - chargesBeforeCurrentYear;

        // ── 5. Transactions after the start date, by head ─────────────────────
        var discounts = await _transactionRepository.Query()
            .AsNoTracking()
            .Where(t => t.TenantId == _tenantId && !t.IsDeleted
                        && t.TransactionDate >= periodStartUtc && t.TransactionDate < toUtc
                        && t.TransactionHead!.UsageFor == UsageFor.BILL_DISCOUNT)
            .GroupBy(t => t.TransactionDate >= currentYearFromUtc)
            .Select(g => new { CurrentYear = g.Key, Amount = g.Sum(t => t.Amount > 0 ? t.Amount : t.DiscountAmount) })
            .ToListAsync(cancellationToken);
        foreach (var d in discounts) Bucket(d.CurrentYear).Discounts += d.Amount;

        var headTotals = await _transactionRepository.Query()
            .AsNoTracking()
            .Where(t => t.TenantId == _tenantId && !t.IsDeleted
                        && t.TransactionDate >= periodStartUtc && t.TransactionDate < toUtc
                        && t.TransactionHead!.UsageFor != UsageFor.BILL_DISCOUNT)
            .GroupBy(t => new
            {
                t.TransactionHead!.Type,
                t.TransactionHead.UsageFor,
                t.TransactionHead.AccountCategory,
                OnCredit = t.PaymentMethod == PaymentMethods.CREDIT,
                CurrentYear = t.TransactionDate >= currentYearFromUtc
            })
            .Select(g => new
            {
                g.Key.Type,
                g.Key.UsageFor,
                g.Key.AccountCategory,
                g.Key.OnCredit,
                g.Key.CurrentYear,
                Count = g.Count(),
                Amount = g.Sum(t => Math.Abs(t.NetAmount))
            })
            .ToListAsync(cancellationToken);

        var lines = new LineTotals();
        foreach (var h in headTotals)
        {
            var category = AccountCategoryResolver.Resolve(h.AccountCategory, h.Type, h.UsageFor);
            var moneyIn = h.Type == TransactionHeadTypes.DEBIT;
            var signedIn = moneyIn ? h.Amount : -h.Amount; // + money in, − money out

            if (AccountCategoryResolver.IsProfitCategory(category))
            {
                Bucket(h.CurrentYear).Add(category, signedIn);
                // Not paid / received yet (pay later)
                if (h.OnCredit)
                {
                    if (moneyIn) lines.OtherReceivables.Add(h.Amount, h.Count);
                    else lines.AccountsPayable.Add(h.Amount, h.Count);
                }
                continue;
            }

            switch (category)
            {
                case AccountCategories.LIABILITY: lines.Creditors.Add(signedIn, h.Count); break;
                case AccountCategories.ASSET: lines.AdvanceDeposit.Add(-signedIn, h.Count); break;
                case AccountCategories.CAPITAL: lines.ShareCapital.Add(signedIn, h.Count); break;
                // FIXED_ASSET: the asset register is the source; EXCLUDED: customer payments / opening cash
            }
        }

        // ── 6. Opening balances and year-end adjustments ──────────────────────
        var entries = await _entryRepository.Query()
            .AsNoTracking()
            .Where(e => e.TenantId == _tenantId && !e.IsDeleted && e.EntryDate <= day)
            .Select(e => new { e.EntryType, e.LineItem, e.Amount, e.ProfitCategory, e.EntryDate })
            .ToListAsync(cancellationToken);

        foreach (var e in entries)
        {
            var line = e.LineItem switch
            {
                BalanceSheetLineItems.ADVANCE_DEPOSIT => lines.AdvanceDeposit,
                BalanceSheetLineItems.OTHER_ASSET => lines.OtherAssets,
                BalanceSheetLineItems.SUNDRY_CREDITORS => lines.Creditors,
                BalanceSheetLineItems.PROVISION => lines.Provisions,
                BalanceSheetLineItems.OTHER_LIABILITY => lines.OtherLiabilities,
                BalanceSheetLineItems.SHARE_CAPITAL => lines.ShareCapital,
                BalanceSheetLineItems.RETAINED_EARNINGS => lines.RetainedEarnings,
                _ => null
            };
            line?.Add(e.Amount, 1);

            // An adjustment's other side is profit: raising an asset is income, raising a liability is expense
            if (e.EntryType == BalanceSheetEntryTypes.ADJUSTMENT && !BalanceSheetLineItems.IsEquity(e.LineItem))
            {
                var profitEffect = BalanceSheetLineItems.IsAsset(e.LineItem) ? e.Amount : -e.Amount;
                Bucket(e.EntryDate >= currentYearFromDay).Adjustments += profitEffect;
            }
        }

        // ── 7. Build the statement ───────────────────────────────────────────
        AddItem(assets, "Property, Plant & Equipment (WDV) / স্থায়ী সম্পদ", ASSET, BalanceSheetSections.NonCurrentAssets, totalWdv, fixedAssets.Count);
        AddItem(assets, "Advance, Deposit & Pre-payments / অগ্রিম ও জামানত", ASSET, BalanceSheetSections.CurrentAssets, lines.AdvanceDeposit);
        AddItem(assets, "Other Assets / অন্যান্য সম্পদ", ASSET, BalanceSheetSections.CurrentAssets, lines.OtherAssets);
        AddItem(assets, "Accounts Receivable (গ্রাহক বকেয়া)", ASSET, BalanceSheetSections.CurrentAssets, receivables.Sum(), receivables.Count);
        AddItem(assets, "Other Receivables / অন্যান্য পাওনা", ASSET, BalanceSheetSections.CurrentAssets, lines.OtherReceivables);
        AddItem(assets, "Cash in Hand / হাতে নগদ", ASSET, BalanceSheetSections.CurrentAssets, cashInHand, 0);

        foreach (var bank in banks)
        {
            bankMovements.TryGetValue(bank.Id, out var movement);
            var balance = bank.OpeningBalance + (movement?.Net ?? 0m);
            var count = movement?.Count ?? 0;
            if (balance >= 0)
                AddItem(assets, $"Cash at Bank - {bank.BankName}", ASSET, BalanceSheetSections.CurrentAssets, balance, count);
            else
                AddItem(liabilities, $"Bank Overdraft - {bank.BankName}", LIABILITY, BalanceSheetSections.CurrentLiabilities, -balance, count);
        }

        AddItem(liabilities, "Sundry Creditors & Loans / বিবিধ পাওনাদার", LIABILITY, BalanceSheetSections.CurrentLiabilities, lines.Creditors);
        AddItem(liabilities, "Provisions & Accrued Expenses / সঞ্চিতি", LIABILITY, BalanceSheetSections.CurrentLiabilities, lines.Provisions);
        AddItem(liabilities, "Accounts Payable (credit purchases)", LIABILITY, BalanceSheetSections.CurrentLiabilities, lines.AccountsPayable);
        AddItem(liabilities, "Customer Advances / গ্রাহক অগ্রিম", LIABILITY, BalanceSheetSections.CurrentLiabilities, -advances.Sum(), advances.Count);
        AddItem(liabilities, "Other Liabilities / অন্যান্য দায়", LIABILITY, BalanceSheetSections.CurrentLiabilities, lines.OtherLiabilities);

        var currentYearProfit = profitCurrent.Net;
        AddItem(equity, "Share Capital / শেয়ার মূলধন", EQUITY, BalanceSheetSections.Equity, lines.ShareCapital);
        AddItem(equity, "Retained Earnings b/f / সঞ্চিত আয় (পূর্ববর্তী)", EQUITY, BalanceSheetSections.Equity,
            lines.RetainedEarnings.Amount + profitBefore.Net, lines.RetainedEarnings.Count);
        AddItem(equity, "Profit & Loss (মুনাফা ও ক্ষতি)", EQUITY, BalanceSheetSections.Equity,
            currentYearProfit, 0);

        response.Assets = assets;
        response.Liabilities = liabilities;
        response.Equity = equity;
        response.ProfitItems = profitCurrent.ToItems();
        response.CurrentYearProfit = currentYearProfit;
        response.TotalAssets = assets.Sum(a => a.Amount);
        response.TotalLiabilities = liabilities.Sum(l => l.Amount);
        response.TotalEquity = equity.Sum(e => e.Amount);
        response.NetWorth = response.TotalAssets - response.TotalLiabilities;
        response.Difference = response.TotalAssets - (response.TotalLiabilities + response.TotalEquity);
        response.TotalTransactions = headTotals.Sum(h => h.Count);
        return response;
    }

    public async Task<BalanceSheetComparativeResponse> GetComparativeBalanceSheetAsync(
        DateTime date1,
        DateTime date2,
        CancellationToken cancellationToken)
    {
        var sheet1 = await GetBalanceSheetAsync(date1, cancellationToken);
        var sheet2 = await GetBalanceSheetAsync(date2, cancellationToken);

        return new BalanceSheetComparativeResponse
        {
            Date1 = date1,
            Date2 = date2,
            Sheet1 = sheet1,
            Sheet2 = sheet2,
            Assets = MergeCategory(sheet1.Assets, sheet2.Assets),
            Liabilities = MergeCategory(sheet1.Liabilities, sheet2.Liabilities),
            Equity = MergeCategory(sheet1.Equity, sheet2.Equity)
        };
    }

    private static List<ComparativeBalanceSheetItemResponse> MergeCategory(
        List<BalanceSheetItemResponse> list1,
        List<BalanceSheetItemResponse> list2)
    {
        // Keep statement order: lines of the later sheet first, then lines only the earlier sheet has
        var lines = list2.Concat(list1).DistinctBy(x => x.AccountName).ToList();
        var dict1 = list1.ToDictionary(x => x.AccountName, x => x.Amount);
        var dict2 = list2.ToDictionary(x => x.AccountName, x => x.Amount);

        return lines.Select(line =>
        {
            dict1.TryGetValue(line.AccountName, out var amt1);
            dict2.TryGetValue(line.AccountName, out var amt2);
            var change = amt2 - amt1;
            return new ComparativeBalanceSheetItemResponse
            {
                AccountName = line.AccountName,
                AccountCategory = line.AccountCategory,
                Section = line.Section,
                Date1Amount = amt1,
                Date2Amount = amt2,
                ChangeAmount = change,
                ChangePercentage = amt1 != 0 ? Math.Round(change / Math.Abs(amt1) * 100, 2) : 0
            };
        }).ToList();
    }

    private static void AddItem(List<BalanceSheetItemResponse> list, string name, string category, string section, LineTotal line)
        => AddItem(list, name, category, section, line.Amount, line.Count);

    private static void AddItem(List<BalanceSheetItemResponse> list, string name, string category, string section, decimal amount, int count)
    {
        if (Math.Abs(amount) < 0.005m) return;
        list.Add(new BalanceSheetItemResponse
        {
            AccountName = name,
            AccountCategory = category,
            Section = section,
            Amount = Math.Round(amount, 2),
            TransactionCount = count
        });
    }

    private sealed class LineTotal
    {
        public decimal Amount { get; private set; }
        public int Count { get; private set; }

        public void Add(decimal amount, int count)
        {
            Amount += amount;
            Count += count;
        }
    }

    private sealed class LineTotals
    {
        public LineTotal AdvanceDeposit { get; } = new();
        public LineTotal OtherAssets { get; } = new();
        public LineTotal OtherReceivables { get; } = new();
        public LineTotal Creditors { get; } = new();
        public LineTotal Provisions { get; } = new();
        public LineTotal AccountsPayable { get; } = new();
        public LineTotal OtherLiabilities { get; } = new();
        public LineTotal ShareCapital { get; } = new();
        public LineTotal RetainedEarnings { get; } = new();
    }

    /// <summary>Profit for one period; expenses are kept as negative numbers.</summary>
    private sealed class ProfitBuckets
    {
        public decimal Charges { get; set; }
        public decimal Discounts { get; set; }
        public decimal OtherIncome { get; set; }
        public decimal DirectExpenses { get; set; }
        public decimal AdminExpenses { get; set; }
        public decimal FinanceExpenses { get; set; }
        public decimal Tax { get; set; }
        public decimal Depreciation { get; set; }
        public decimal Adjustments { get; set; }

        public decimal Net => Charges - Discounts + OtherIncome + DirectExpenses + AdminExpenses
                              + FinanceExpenses + Tax - Depreciation + Adjustments;

        public void Add(string category, decimal signedIn)
        {
            switch (category)
            {
                case AccountCategories.INCOME: OtherIncome += signedIn; break;
                case AccountCategories.EXPENSE_DIRECT: DirectExpenses += signedIn; break;
                case AccountCategories.EXPENSE_FINANCE: FinanceExpenses += signedIn; break;
                case AccountCategories.TAX: Tax += signedIn; break;
                default: AdminExpenses += signedIn; break;
            }
        }

        public List<BalanceSheetItemResponse> ToItems()
        {
            var items = new List<BalanceSheetItemResponse>();
            AddItem(items, "Rent & labour charges / ভাড়া ও লেবার চার্জ", "Profit", BalanceSheetSections.Profit, Charges, 0);
            AddItem(items, "Less: Bill discounts / ছাড়", "Profit", BalanceSheetSections.Profit, -Discounts, 0);
            AddItem(items, "Other income / অন্যান্য আয়", "Profit", BalanceSheetSections.Profit, OtherIncome, 0);
            AddItem(items, "Less: Direct expenses / প্রত্যক্ষ ব্যয়", "Profit", BalanceSheetSections.Profit, DirectExpenses, 0);
            AddItem(items, "Less: Administrative expenses / প্রশাসনিক ব্যয়", "Profit", BalanceSheetSections.Profit, AdminExpenses, 0);
            AddItem(items, "Less: Finance expenses / আর্থিক ব্যয়", "Profit", BalanceSheetSections.Profit, FinanceExpenses, 0);
            AddItem(items, "Less: Depreciation / অবচয়", "Profit", BalanceSheetSections.Profit, -Depreciation, 0);
            AddItem(items, "Year-end adjustments / সমন্বয়", "Profit", BalanceSheetSections.Profit, Adjustments, 0);
            AddItem(items, "Less: Income tax / আয়কর", "Profit", BalanceSheetSections.Profit, Tax, 0);
            return items;
        }
    }
}
