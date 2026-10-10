using Application.Contractors;
using Application.Contractors.Authentication;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;
using Application.Services.Common;

namespace Application.Services
{
    public class CashBookService : ICashBookService
    {
        private readonly IRepository<Transaction, Guid> _transactionRepository;
        private readonly IRepository<BankTransaction, long> _bankTransactionRepository;
        private readonly IBalanceCalculatorService _balanceCalculatorService;
        private readonly Guid _tenantId;

        public CashBookService(
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
        /// Daily Cash Book. Internally this is a one-day range, so daily and range reports
        /// always produce identical figures for the same day.
        /// </summary>
        public async Task<CashBookResponse> GetCashBookAsync(DateTime reportDate, CancellationToken cancellationToken = default)
        {
            var result = await BuildAsync(ReportDateWindow.ForDay(reportDate), cancellationToken);
            var day = result.Days.FirstOrDefault();

            return new CashBookResponse
            {
                OpeningBalance = result.OpeningBalance,
                Items = day?.Items ?? new List<CashBookItemResponse>(),
                TotalDebit = result.TotalDebit,
                TotalCredit = result.TotalCredit,
                ClosingBalance = result.ClosingBalance
            };
        }

        public Task<CashBookRangeResponse> GetCashBookRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            return BuildAsync(ReportDateWindow.ForRange(fromDate, toDate), cancellationToken);
        }

        private async Task<CashBookRangeResponse> BuildAsync(ReportDateWindow window, CancellationToken cancellationToken)
        {
            // Opening cash balance at the start of the first day
            var openingBalance = await _balanceCalculatorService.GetOpeningBalanceAsync(
                window.FromUtc, window.OpeningAnchorUtc, false, cancellationToken);

            // Cash revenue / expense transactions in the window (projected, read-only)
            var transactions = await _transactionRepository.Query()
                .AsNoTracking()
                .Where(t => t.TenantId == _tenantId
                         && t.TransactionDate >= window.FromUtc && t.TransactionDate < window.ToUtc
                         && t.PaymentMethod == PaymentMethods.CASH
                         && !t.IsDeleted && !t.IsArchived
                         && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE
                         && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
                .Select(t => new CashTx(
                    t.TransactionDate,
                    t.TransactionHeadId,
                    t.TransactionHead!.Name,
                    t.TransactionHead!.Type,
                    t.NetAmount,
                    t.TransactionHead!.TransactionGroupId,
                    t.TransactionHead!.TransactionGroup != null ? t.TransactionHead!.TransactionGroup.Name : null))
                .ToListAsync(cancellationToken);

            // Internal cash <-> bank transfers (contra) in the window
            var transfers = await _bankTransactionRepository.Query()
                .AsNoTracking()
                .Where(bt => bt.TenantId == _tenantId
                          && bt.TransactionDate >= window.FromUtc && bt.TransactionDate < window.ToUtc
                          && bt.IsActive && !bt.IsArchived && !bt.IsDeleted
                          && (bt.SourceType == null || bt.SourceType == BankSourceTypes.CASH))
                .Select(bt => new CashTransfer(bt.TransactionDate, bt.TransactionType, bt.Amount))
                .ToListAsync(cancellationToken);

            // Days (after the first) on which an explicit opening balance re-anchors cash
            var resetDays = (await _balanceCalculatorService.GetOpeningBalanceResetDaysAsync(
                    window.FromUtc, window.ToUtc, cancellationToken))
                .Where(d => d > window.FromDate)
                .ToHashSet();

            var txByDay = transactions.ToLookup(t => ReportDateWindow.ToLocalDay(t.TransactionDate));
            var transfersByDay = transfers.ToLookup(t => ReportDateWindow.ToLocalDay(t.TransactionDate));

            var activeDays = txByDay.Select(g => g.Key)
                .Concat(transfersByDay.Select(g => g.Key))
                .Concat(resetDays)
                .Where(d => d >= window.FromDate && d <= window.ToDate)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var days = new List<CashBookDayResponse>();
            var running = openingBalance;
            decimal totalAdjustment = 0;

            foreach (var day in activeDays)
            {
                var dayOpening = running;
                decimal adjustment = 0;
                var isReset = resetDays.Contains(day);

                if (isReset)
                {
                    // Re-read the opening exactly as the daily report would for this day
                    var reanchored = await _balanceCalculatorService.GetCashOpeningBalanceAsync(
                        ReportDateWindow.DayStartUtc(day), ReportDateWindow.OpeningAnchorFor(day), cancellationToken);
                    adjustment = reanchored - running;
                    dayOpening = reanchored;
                    totalAdjustment += adjustment;
                }

                var items = BuildItems(txByDay[day], transfersByDay[day], dayOpening, withRunningBalance: true);
                var dayDebit = items.Sum(i => i.DebitAmount);
                var dayCredit = items.Sum(i => i.CreditAmount);
                var dayClosing = dayOpening + dayDebit - dayCredit;

                days.Add(new CashBookDayResponse
                {
                    Date = day,
                    OpeningBalance = dayOpening,
                    IsOpeningBalanceReset = isReset,
                    OpeningBalanceAdjustment = adjustment,
                    Items = items,
                    TotalDebit = dayDebit,
                    TotalCredit = dayCredit,
                    ClosingBalance = dayClosing
                });

                running = dayClosing;
            }

            return new CashBookRangeResponse
            {
                FromDate = window.FromDate,
                ToDate = window.ToDate,
                OpeningBalance = openingBalance,
                Days = days,
                HeadSummary = BuildItems(transactions, transfers, 0, withRunningBalance: false),
                TotalDebit = days.Sum(d => d.TotalDebit),
                TotalCredit = days.Sum(d => d.TotalCredit),
                OpeningBalanceAdjustment = totalAdjustment,
                ClosingBalance = running
            };
        }

        /// <summary>
        /// Groups cash transactions by transaction head (or its Transaction Group), followed by contra bank transfers.
        /// Money IN = Debit, Money OUT = Credit (physical cash drawer perspective).
        /// </summary>
        private static List<CashBookItemResponse> BuildItems(
            IEnumerable<CashTx> transactions,
            IEnumerable<CashTransfer> transfers,
            decimal openingBalance,
            bool withRunningBalance)
        {
            var items = new List<CashBookItemResponse>();
            var runningBalance = openingBalance;

            // Heads mapped to a Transaction Group are merged into one row (Income and Expense together);
            // unmapped heads stay one row per head as before.
            var headGroups = transactions
                .GroupBy(t => t.GroupId.HasValue && t.GroupName != null
                    ? new { Id = t.GroupId.Value, Name = t.GroupName, IsGroup = true }
                    : new { Id = t.TransactionHeadId, Name = t.HeadName ?? "Unknown", IsGroup = false })
                .Select(g => new
                {
                    g.Key.Id,
                    g.Key.Name,
                    g.Key.IsGroup,
                    TransactionType = g.Key.IsGroup ? "GROUP" : (g.First().HeadType ?? "Unknown"),
                    Count = g.Count(),
                    DebitAmount = g.Where(t => t.HeadType == TransactionHeadTypes.DEBIT).Sum(t => Math.Abs(t.NetAmount)),
                    CreditAmount = g.Where(t => t.HeadType != TransactionHeadTypes.DEBIT).Sum(t => Math.Abs(t.NetAmount))
                })
                .OrderBy(g => g.Name);

            // Money IN = Debit, Money OUT = Credit
            foreach (var group in headGroups)
            {
                runningBalance += group.DebitAmount - group.CreditAmount;

                items.Add(new CashBookItemResponse
                {
                    TransactionHeadId = group.Id,
                    TransactionHeadName = group.Name,
                    TransactionType = group.TransactionType,
                    IsGroup = group.IsGroup,
                    TransactionCount = group.Count,
                    DebitAmount = group.DebitAmount,
                    CreditAmount = group.CreditAmount,
                    Balance = withRunningBalance ? runningBalance : 0
                });
            }

            var transferGroups = transfers
                .GroupBy(t => t.TransactionType)
                .Select(g => new { TransactionType = g.Key, Count = g.Count(), TotalAmount = g.Sum(t => t.Amount) })
                .OrderBy(g => g.TransactionType);

            foreach (var group in transferGroups)
            {
                // Bank Deposit: cash leaves the drawer -> Credit
                // Bank Withdrawal: cash enters the drawer -> Debit
                var isDeposit = group.TransactionType == BankTransactionTypes.Deposit;
                var debitAmount = !isDeposit ? group.TotalAmount : 0;
                var creditAmount = isDeposit ? group.TotalAmount : 0;
                runningBalance += debitAmount - creditAmount;

                items.Add(new CashBookItemResponse
                {
                    TransactionHeadId = Guid.Empty,
                    TransactionHeadName = isDeposit ? "Bank Deposit (নগদ জমা)" : "Bank Withdrawal (নগদ উত্তোলন)",
                    TransactionType = group.TransactionType,
                    TransactionCount = group.Count,
                    DebitAmount = debitAmount,
                    CreditAmount = creditAmount,
                    Balance = withRunningBalance ? runningBalance : 0
                });
            }

            return items;
        }

        private sealed record CashTx(DateTime TransactionDate, Guid TransactionHeadId, string? HeadName, string? HeadType, decimal NetAmount, Guid? GroupId, string? GroupName);

        private sealed record CashTransfer(DateTime TransactionDate, string TransactionType, decimal Amount);
    }
}
