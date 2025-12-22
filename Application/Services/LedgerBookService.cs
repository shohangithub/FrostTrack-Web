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
            var transactionsBeforeDate = await _transactionRepository.Query()
                .Include(t => t.TransactionHead)
                .Where(t => t.TransactionDate < reportDate && !t.IsArchived)
                .ToListAsync(cancellationToken);

            var openingBalance = transactionsBeforeDate.Sum(t =>
                t.TransactionHead?.Type == TransactionHeadTypes.CREDIT ? t.NetAmount : -t.NetAmount);

            // Get transactions for the report date
            var transactions = await _transactionRepository.Query()
                .Include(t => t.TransactionHead)
                .Where(t => t.TransactionDate.Date == reportDate.Date && !t.IsArchived)
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
