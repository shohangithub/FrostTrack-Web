using Application.Common;
using Application.Contractors;
using Application.Contractors.Authentication;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;
using Application.Services.Common;

namespace Application.Services
{
    /// <summary>
    /// Ledger Book per Transaction Group. Sides follow the Trial Balance so a group's ledger
    /// ties to its Trial Balance row: money IN (head Type DEBIT) -> Credit, money OUT (head Type CREDIT) -> Debit.
    /// Balances are Credit - Debit (positive = Cr, negative = Dr).
    /// </summary>
    public class LedgerBookService : ILedgerBookService
    {
        private readonly IRepository<Transaction, Guid> _transactionRepository;
        private readonly IRepository<TransactionGroup, Guid> _groupRepository;
        private readonly Guid _tenantId;

        public LedgerBookService(
            IRepository<Transaction, Guid> transactionRepository,
            IRepository<TransactionGroup, Guid> groupRepository,
            ITenantProvider tenantProvider)
        {
            _transactionRepository = transactionRepository;
            _groupRepository = groupRepository;
            _tenantId = tenantProvider.GetTenantId();
        }

        public async Task<LedgerBookSummaryResponse> GetSummaryAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            var window = ReportDateWindow.ForRange(fromDate, toDate);

            var groups = await _groupRepository.Query()
                .Where(g => !g.IsDeleted)
                .OrderBy(g => g.SortOrder)
                .ThenBy(g => g.Name)
                .Select(g => new { g.Id, g.Code, g.Name, g.IsActive })
                .ToListAsync(cancellationToken);

            var opening = await GroupedTransactions()
                .Where(t => t.TransactionDate < window.FromUtc)
                .GroupBy(t => t.TransactionHead!.TransactionGroupId!.Value)
                .Select(g => new
                {
                    GroupId = g.Key,
                    Balance = g.Sum(t => t.TransactionHead!.Type == TransactionHeadTypes.DEBIT ? Math.Abs(t.NetAmount) : -Math.Abs(t.NetAmount))
                })
                .ToDictionaryAsync(x => x.GroupId, x => x.Balance, cancellationToken);

            var period = await GroupedTransactions()
                .Where(t => t.TransactionDate >= window.FromUtc && t.TransactionDate < window.ToUtc)
                .GroupBy(t => t.TransactionHead!.TransactionGroupId!.Value)
                .Select(g => new
                {
                    GroupId = g.Key,
                    Debit = g.Sum(t => t.TransactionHead!.Type == TransactionHeadTypes.DEBIT ? 0 : Math.Abs(t.NetAmount)),
                    Credit = g.Sum(t => t.TransactionHead!.Type == TransactionHeadTypes.DEBIT ? Math.Abs(t.NetAmount) : 0),
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.GroupId, cancellationToken);

            var items = new List<LedgerBookGroupSummaryItem>();
            foreach (var group in groups)
            {
                var openingBalance = opening.GetValueOrDefault(group.Id);
                period.TryGetValue(group.Id, out var movement);

                // Inactive groups are listed only when they still carry a balance or activity
                if (!group.IsActive && openingBalance == 0 && movement == null)
                    continue;

                var debit = movement?.Debit ?? 0;
                var credit = movement?.Credit ?? 0;

                items.Add(new LedgerBookGroupSummaryItem
                {
                    GroupId = group.Id,
                    GroupCode = group.Code,
                    GroupName = group.Name,
                    OpeningBalance = openingBalance,
                    DebitAmount = debit,
                    CreditAmount = credit,
                    ClosingBalance = openingBalance + credit - debit,
                    TransactionCount = movement?.Count ?? 0
                });
            }

            return new LedgerBookSummaryResponse
            {
                FromDate = window.FromDate,
                ToDate = window.ToDate,
                Groups = items,
                TotalOpeningBalance = items.Sum(i => i.OpeningBalance),
                TotalDebit = items.Sum(i => i.DebitAmount),
                TotalCredit = items.Sum(i => i.CreditAmount),
                TotalClosingBalance = items.Sum(i => i.ClosingBalance)
            };
        }

        public async Task<LedgerBookGroupResponse> GetGroupLedgerAsync(Guid groupId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            var window = ReportDateWindow.ForRange(fromDate, toDate);

            var group = await _groupRepository.Query()
                .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken)
                ?? throw new NotFoundException("Transaction group not found");

            var groupTransactions = GroupedTransactions()
                .Where(t => t.TransactionHead!.TransactionGroupId == groupId);

            var openingBalance = await groupTransactions
                .Where(t => t.TransactionDate < window.FromUtc)
                .SumAsync(t => t.TransactionHead!.Type == TransactionHeadTypes.DEBIT ? Math.Abs(t.NetAmount) : -Math.Abs(t.NetAmount), cancellationToken);

            var transactions = await groupTransactions
                .Where(t => t.TransactionDate >= window.FromUtc && t.TransactionDate < window.ToUtc)
                .OrderBy(t => t.TransactionDate)
                .ThenBy(t => t.CreatedTime)
                .Select(t => new
                {
                    t.Id,
                    t.TransactionDate,
                    t.TransactionCode,
                    t.Description,
                    HeadName = t.TransactionHead!.Name,
                    HeadType = t.TransactionHead!.Type,
                    t.PaymentMethod,
                    BankName = t.Bank != null ? t.Bank.BankName : null,
                    t.PaymentReference,
                    t.NetAmount
                })
                .ToListAsync(cancellationToken);

            var items = new List<LedgerBookItemResponse>();
            var running = openingBalance;

            foreach (var t in transactions)
            {
                var amount = Math.Abs(t.NetAmount);
                var isMoneyIn = t.HeadType == TransactionHeadTypes.DEBIT;
                var debit = isMoneyIn ? 0 : amount;
                var credit = isMoneyIn ? amount : 0;
                running += credit - debit;

                items.Add(new LedgerBookItemResponse
                {
                    Id = t.Id,
                    Date = t.TransactionDate,
                    TransactionCode = t.TransactionCode,
                    Description = t.Description ?? string.Empty,
                    TransactionHeadName = t.HeadName,
                    PaymentMethod = t.PaymentMethod,
                    BankName = t.BankName,
                    ReferenceNo = t.PaymentReference,
                    DebitAmount = debit,
                    CreditAmount = credit,
                    Balance = running
                });
            }

            return new LedgerBookGroupResponse
            {
                FromDate = window.FromDate,
                ToDate = window.ToDate,
                GroupId = group.Id,
                GroupCode = group.Code,
                GroupName = group.Name,
                OpeningBalance = openingBalance,
                Items = items,
                TotalDebit = items.Sum(i => i.DebitAmount),
                TotalCredit = items.Sum(i => i.CreditAmount),
                ClosingBalance = running
            };
        }

        /// <summary>
        /// Transactions of grouped heads, filtered the same way as the Trial Balance / General Ledger.
        /// </summary>
        private IQueryable<Transaction> GroupedTransactions() =>
            _transactionRepository.Query()
                .Where(t => t.TenantId == _tenantId
                         && !t.IsDeleted && !t.IsArchived
                         && t.PaymentMethod != PaymentMethods.CREDIT
                         && t.TransactionHead!.TransactionGroupId != null
                         && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE
                         && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE);
    }
}
