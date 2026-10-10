using Application.Contractors;
using Application.Contractors.Authentication;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;
using Application.Services.Common;

namespace Application.Services;

public class GeneralLedgerService : IGeneralLedgerService
{
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<BankTransaction, long> _bankTransactionRepository;
    private readonly IBalanceCalculatorService _balanceCalculatorService;
    private readonly Guid _tenantId;

    public GeneralLedgerService(
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

    /// <summary>
    /// Daily General Ledger. Internally a one-day range so daily and range figures always agree.
    /// </summary>
    public async Task<GeneralLedgerResponse> GetGeneralLedgerAsync(DateTime reportDate, CancellationToken cancellationToken = default)
    {
        var result = await BuildAsync(ReportDateWindow.ForDay(reportDate), cancellationToken);

        return new GeneralLedgerResponse
        {
            ReportDate = reportDate,
            OpeningBalance = result.OpeningBalance,
            Items = result.Days.SelectMany(d => d.Items).OrderBy(i => i.Date).ToList(),
            AccountSummary = result.AccountSummary,
            TotalDebit = result.TotalDebit,
            TotalCredit = result.TotalCredit,
            ClosingBalance = result.ClosingBalance
        };
    }

    public Task<GeneralLedgerRangeResponse> GetGeneralLedgerRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return BuildAsync(ReportDateWindow.ForRange(fromDate, toDate), cancellationToken);
    }

    private async Task<GeneralLedgerRangeResponse> BuildAsync(ReportDateWindow window, CancellationToken cancellationToken)
    {
        // 1. Initial Opening Balance (Cash + Bank) as of window start
        var openingBalance = await _balanceCalculatorService.GetOpeningBalanceAsync(
            window.FromUtc, window.OpeningAnchorUtc, true, cancellationToken);

        // 2. Fetch nominal transactions in window
        var transactions = await _transactionRepository.Query()
            .AsNoTracking()
            .Include(t => t.TransactionHead)
                .ThenInclude(h => h!.TransactionGroup)
            .Include(t => t.Bank)
            .Where(t => t.TenantId == _tenantId
                && t.TransactionDate >= window.FromUtc && t.TransactionDate < window.ToUtc
                && !t.IsDeleted && !t.IsArchived
                && t.PaymentMethod != PaymentMethods.CREDIT
                && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE
                && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.CreatedTime)
            .ToListAsync(cancellationToken);

        // 3. Fetch bank transactions in window (excluding customer payments, delivery collections, and linked transactions)
        var bankTransactions = await _bankTransactionRepository.Query()
            .AsNoTracking()
            .Include(bt => bt.Bank)
            .Where(bt => bt.TenantId == _tenantId
                && bt.TransactionDate >= window.FromUtc && bt.TransactionDate < window.ToUtc
                && bt.IsActive && !bt.IsDeleted && !bt.IsArchived
                && bt.SourceType != BankSourceTypes.CUSTOMER_PAYMENT
                && bt.SourceType != BankSourceTypes.DELIVERY
                && bt.TransactionId == null)
            .OrderBy(bt => bt.TransactionDate)
            .ThenBy(bt => bt.CreatedTime)
            .ToListAsync(cancellationToken);

        // Check for opening balance reset days (e.g. season transitions)
        var resetDays = (await _balanceCalculatorService.GetOpeningBalanceResetDaysAsync(
                window.FromUtc, window.ToUtc, cancellationToken))
            .Where(d => d > window.FromDate)
            .ToHashSet();

        // Convert transactions and bank transactions into GeneralLedgerItemResponse items
        var allItems = new List<GeneralLedgerItemResponse>();

        foreach (var transaction in transactions)
        {
            var isMoneyIn = transaction.TransactionHead?.Type == TransactionHeadTypes.DEBIT;
            var netAmount = Math.Abs(transaction.NetAmount);
            var debitAmount = isMoneyIn ? netAmount : 0;
            var creditAmount = !isMoneyIn ? netAmount : 0;

            var isBankOrCheque = transaction.PaymentMethod != PaymentMethods.CASH || transaction.BankId.HasValue;
            var headName = transaction.TransactionHead?.Name ?? "Unknown";
            var groupName = transaction.TransactionHead?.TransactionGroup?.Name;
            // Mapped heads show under their Transaction Group; the head name is kept for display underneath
            var accountName = groupName ?? headName;
            if (transaction.Bank != null)
            {
                accountName = $"{accountName} ({transaction.Bank.BankName})";
            }

            var description = transaction.Description ?? string.Empty;
            if (isBankOrCheque)
            {
                var extraParts = new List<string>();
                if (transaction.Bank != null && !description.Contains(transaction.Bank.BankName))
                    extraParts.Add(transaction.Bank.BankName);
                if (!string.IsNullOrEmpty(transaction.PaymentMethod) && transaction.PaymentMethod != PaymentMethods.CASH && !description.Contains(transaction.PaymentMethod))
                    extraParts.Add(transaction.PaymentMethod);
                if (!string.IsNullOrEmpty(transaction.PaymentReference) && !description.Contains(transaction.PaymentReference))
                    extraParts.Add($"Ref: {transaction.PaymentReference}");

                if (extraParts.Any())
                {
                    description = $"{description} [{string.Join(" - ", extraParts)}]";
                }
            }

            allItems.Add(new GeneralLedgerItemResponse
            {
                Id = transaction.Id.ToString(),
                Date = transaction.TransactionDate,
                TransactionCode = transaction.TransactionCode,
                Description = description,
                AccountName = accountName,
                HeadName = headName,
                GroupName = groupName,
                AccountType = isBankOrCheque ? "Bank" : "Cash",
                TransactionType = transaction.TransactionHead?.Type ?? "Unknown",
                PaymentMethod = transaction.PaymentMethod,
                ReferenceNo = transaction.PaymentReference,
                DebitAmount = debitAmount,
                CreditAmount = creditAmount
            });
        }

        foreach (var bankTransaction in bankTransactions)
        {
            var isDeposit = bankTransaction.TransactionType == BankTransactionTypes.Deposit;
            var isCashContra = bankTransaction.SourceType == BankSourceTypes.CASH || string.IsNullOrEmpty(bankTransaction.SourceType);

            if (isCashContra)
            {
                // Contra Leg 1: Bank
                var bankDebit = isDeposit ? bankTransaction.Amount : 0;
                var bankCredit = !isDeposit ? bankTransaction.Amount : 0;

                allItems.Add(new GeneralLedgerItemResponse
                {
                    Id = $"{bankTransaction.Id}-BANK",
                    Date = bankTransaction.TransactionDate,
                    TransactionCode = $"BANK-{bankTransaction.Id}",
                    Description = isDeposit 
                        ? $"Bank Deposit from Cash ({bankTransaction.Bank?.BankName})" 
                        : $"Bank Withdrawal to Cash ({bankTransaction.Bank?.BankName})",
                    AccountName = $"{bankTransaction.Bank?.BankName} - {bankTransaction.Bank?.AccountNumber}",
                    AccountType = "Bank",
                    TransactionType = "Contra",
                    PaymentMethod = "Bank",
                    ReferenceNo = bankTransaction.Reference,
                    DebitAmount = bankDebit,
                    CreditAmount = bankCredit
                });

                // Contra Leg 2: Cash
                var cashDebit = !isDeposit ? bankTransaction.Amount : 0;
                var cashCredit = isDeposit ? bankTransaction.Amount : 0;

                allItems.Add(new GeneralLedgerItemResponse
                {
                    Id = $"{bankTransaction.Id}-CASH",
                    Date = bankTransaction.TransactionDate,
                    TransactionCode = $"CASH-{bankTransaction.Id}",
                    Description = isDeposit 
                        ? $"Cash transferred to Bank ({bankTransaction.Bank?.BankName})" 
                        : $"Cash received from Bank ({bankTransaction.Bank?.BankName})",
                    AccountName = "Cash in Hand (নগদ তহবিল)",
                    AccountType = "Cash",
                    TransactionType = "Contra",
                    PaymentMethod = "Cash",
                    ReferenceNo = bankTransaction.Reference,
                    DebitAmount = cashDebit,
                    CreditAmount = cashCredit
                });
            }
            else
            {
                var debitAmount = isDeposit ? bankTransaction.Amount : 0;
                var creditAmount = !isDeposit ? bankTransaction.Amount : 0;

                allItems.Add(new GeneralLedgerItemResponse
                {
                    Id = bankTransaction.Id.ToString(),
                    Date = bankTransaction.TransactionDate,
                    TransactionCode = $"BANK-{bankTransaction.Id}",
                    Description = bankTransaction.Description ?? "Bank Transaction",
                    AccountName = $"{bankTransaction.Bank?.BankName} - {bankTransaction.Bank?.AccountNumber}",
                    AccountType = "Bank",
                    TransactionType = bankTransaction.TransactionType,
                    PaymentMethod = "Bank Transfer",
                    ReferenceNo = bankTransaction.Reference,
                    DebitAmount = debitAmount,
                    CreditAmount = creditAmount
                });
            }
        }

        var itemsByDay = allItems.ToLookup(i => ReportDateWindow.ToLocalDay(i.Date));
        var activeDays = itemsByDay.Select(g => g.Key)
            .Concat(resetDays)
            .Where(d => d >= window.FromDate && d <= window.ToDate)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var days = new List<GeneralLedgerDayResponse>();
        var running = openingBalance;
        decimal totalAdjustment = 0;

        foreach (var day in activeDays)
        {
            var dayOpening = running;
            decimal adjustment = 0;
            var isReset = resetDays.Contains(day);

            if (isReset)
            {
                var reanchored = await _balanceCalculatorService.GetOpeningBalanceAsync(
                    ReportDateWindow.DayStartUtc(day), ReportDateWindow.OpeningAnchorFor(day), true, cancellationToken);
                adjustment = reanchored - running;
                dayOpening = reanchored;
                totalAdjustment += adjustment;
            }

            var dayItems = itemsByDay[day].OrderBy(i => i.Date).ToList();
            var dayDebit = dayItems.Sum(i => i.DebitAmount);
            var dayCredit = dayItems.Sum(i => i.CreditAmount);
            var dayClosing = dayOpening + dayDebit - dayCredit;

            days.Add(new GeneralLedgerDayResponse
            {
                Date = day,
                OpeningBalance = dayOpening,
                IsOpeningBalanceReset = isReset,
                OpeningBalanceAdjustment = adjustment,
                Items = dayItems,
                TotalDebit = dayDebit,
                TotalCredit = dayCredit,
                ClosingBalance = dayClosing
            });

            running = dayClosing;
        }

        // Period summary: one row per Transaction Group (or head / bank account when not grouped)
        var accountSummary = days
            .SelectMany(d => d.Items)
            .GroupBy(i => new
            {
                Name = i.GroupName ?? i.HeadName ?? i.AccountName,
                IsGroup = i.GroupName != null
            })
            .Select(g => new GeneralLedgerAccountSummaryResponse
            {
                AccountName = g.Key.Name,
                IsGroup = g.Key.IsGroup,
                TransactionCount = g.Count(),
                DebitAmount = g.Sum(i => i.DebitAmount),
                CreditAmount = g.Sum(i => i.CreditAmount)
            })
            .OrderByDescending(a => a.IsGroup)
            .ThenBy(a => a.AccountName)
            .ToList();

        return new GeneralLedgerRangeResponse
        {
            FromDate = window.FromDate,
            ToDate = window.ToDate,
            OpeningBalance = openingBalance,
            Days = days,
            AccountSummary = accountSummary,
            TotalDebit = days.Sum(d => d.TotalDebit),
            TotalCredit = days.Sum(d => d.TotalCredit),
            OpeningBalanceAdjustment = totalAdjustment,
            ClosingBalance = running
        };
    }
}
