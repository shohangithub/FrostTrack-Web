using Application.Contractors;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class CashBookService : ICashBookService
    {
        private readonly IRepository<Transaction, Guid> _transactionRepository;

        public CashBookService(
            IRepository<Transaction, Guid> transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<CashBookResponse> GetCashBookAsync(DateTime reportDate, CancellationToken cancellationToken = default)
        {
            // Get opening balance (cash transactions before report date)
            var transactionsBeforeDate = await _transactionRepository.Query()
                .Include(t => t.TransactionHead)
                .Where(t => t.TransactionDate < reportDate && t.PaymentMethod == PaymentMethods.CASH && t.IsArchived == false)
                .ToListAsync(cancellationToken);

            var openingBalance = transactionsBeforeDate.Sum(t =>
                t.TransactionHead?.Type == TransactionHeadTypes.CREDIT ? t.NetAmount : -t.NetAmount);

            // Get cash transactions for the report date
            var transactions = await _transactionRepository.Query()
                .Include(t => t.TransactionHead)
                .Where(t => t.TransactionDate.Date == reportDate.Date && t.PaymentMethod == PaymentMethods.CASH && t.IsArchived == false)
                .ToListAsync(cancellationToken);

            // Group by transaction head and sum amounts
            var groupedTransactions = transactions
                .GroupBy(t => new
                {
                    t.TransactionHeadId,
                    TransactionHeadName = t.TransactionHead?.Name ?? "Unknown",
                    TransactionType = t.TransactionHead?.Type ?? "Unknown"
                })
                .Select(g => new
                {
                    g.Key.TransactionHeadId,
                    g.Key.TransactionHeadName,
                    g.Key.TransactionType,
                    Count = g.Count(),
                    TotalAmount = g.Sum(t => t.NetAmount)
                })
                .OrderBy(g => g.TransactionHeadName)
                .ToList();

            var items = new List<CashBookItemResponse>();
            var runningBalance = openingBalance;

            foreach (var group in groupedTransactions)
            {
                var isCredit = group.TransactionType == TransactionHeadTypes.CREDIT;
                var debitAmount = !isCredit ? group.TotalAmount : 0;
                var creditAmount = isCredit ? group.TotalAmount : 0;

                runningBalance += creditAmount - debitAmount;

                items.Add(new CashBookItemResponse
                {
                    TransactionHeadId = group.TransactionHeadId,
                    TransactionHeadName = group.TransactionHeadName,
                    TransactionType = group.TransactionType,
                    TransactionCount = group.Count,
                    DebitAmount = debitAmount,
                    CreditAmount = creditAmount,
                    Balance = runningBalance
                });
            }

            var totalDebit = items.Sum(i => i.DebitAmount);
            var totalCredit = items.Sum(i => i.CreditAmount);
            var closingBalance = openingBalance + totalCredit - totalDebit;

            return new CashBookResponse
            {
                OpeningBalance = openingBalance,
                Items = items,
                TotalDebit = totalDebit,
                TotalCredit = totalCredit,
                ClosingBalance = closingBalance
            };
        }
    }
}
