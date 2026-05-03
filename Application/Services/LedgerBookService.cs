using Application.Contractors;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class LedgerBookService : ILedgerBookService
    {
        private readonly IRepository<Transaction, Guid> _transactionRepository;

        public LedgerBookService(
            IRepository<Transaction, Guid> transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<LedgerBookResponse> GetGeneralLedgerAsync(DateTime reportDate, CancellationToken cancellationToken = default)
        {
            // Get opening balance (all transactions before report date)
            var fromLocal = reportDate.Date;
            var fromUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local)
                .ToUniversalTime();

            var toLocalExclusive = fromLocal.AddDays(1);
            var toUtc = DateTime.SpecifyKind(toLocalExclusive, DateTimeKind.Local)
                .ToUniversalTime();

            var dateWithUTCTime = reportDate.GetDateUtcTime();

            var lastOpeningBalance = await _transactionRepository.Query()
                .Include(t => t.TransactionHead)
                .Where(t =>
                    !t.IsDeleted &&
                    !t.IsArchived &&
                    t.TransactionHead!.UsageFor == UsageFor.OPENING_BALANCE
                     && t.TransactionDate < dateWithUTCTime
                    )
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new
                {
                    t.TransactionDate,
                    t.NetAmount
                })
                .FirstOrDefaultAsync(cancellationToken);

            var openingDate = lastOpeningBalance?.TransactionDate ?? dateWithUTCTime;

            var previousAmount = await _transactionRepository.Query()
                .Include(t => t.TransactionHead)
                .Where(t =>
                    !t.IsDeleted &&
                    !t.IsArchived &&
                    t.TransactionDate >= openingDate &&
                    t.TransactionDate < fromUtc &&
                    t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
                .SumAsync(t => t.NetAmount, cancellationToken);

            var openingBalance = (lastOpeningBalance?.NetAmount ?? 0) + previousAmount;

            // Get transactions for the report date
            var transactions = await _transactionRepository.Query()
                .Include(t => t.TransactionHead)
                .Where(t => t.TransactionDate >= fromUtc && t.TransactionDate < toUtc && !t.IsDeleted && !t.IsArchived && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
                .OrderBy(t => t.CreatedTime)
                .ToListAsync(cancellationToken);

            var items = new List<LedgerBookItemResponse>();
            decimal totalDebit = 0;
            decimal totalCredit = 0;

            foreach (var transaction in transactions)
            {
                var isCredit = transaction.TransactionHead?.Type == TransactionHeadTypes.CREDIT;
                var debitAmount = isCredit ? 0 : transaction.NetAmount;
                var creditAmount = isCredit ? transaction.NetAmount : 0;

                totalDebit += debitAmount;
                totalCredit += creditAmount;

                items.Add(new LedgerBookItemResponse
                {
                    Id = transaction.Id,
                    Date = transaction.TransactionDate,
                    TransactionCode = transaction.TransactionCode,
                    Description = transaction.Description,
                    TransactionHeadName = transaction.TransactionHead?.Name ?? "Unknown",
                    TransactionType = transaction.TransactionHead?.Type ?? "Unknown",
                    PaymentMethod = transaction.PaymentMethod,
                    ReferenceNo = transaction.PaymentReference,
                    DebitAmount = (-1) * debitAmount,
                    CreditAmount = creditAmount,
                    Balance = 0 // Not needed for general ledger
                });
            }

            var closingBalance = openingBalance + totalCredit + totalDebit;

            return new LedgerBookResponse
            {
                ReportDate = reportDate,
                OpeningBalance = openingBalance,
                Items = items,
                TotalDebit = (-1) * totalDebit,
                TotalCredit = totalCredit,
                ClosingBalance = closingBalance
            };
        }
    }
}
