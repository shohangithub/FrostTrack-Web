using Application.Contractors;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class CashBookService : ICashBookService
    {
        private readonly IRepository<Transaction, Guid> _transactionRepository;
        private readonly IRepository<BankTransaction, long> _bankTransactionRepository;
        public CashBookService(
            IRepository<Transaction, Guid> transactionRepository,
            IRepository<BankTransaction, long> bankTransactionRepository)
        {
            _transactionRepository = transactionRepository;
            _bankTransactionRepository = bankTransactionRepository;
        }

        public async Task<CashBookResponse> GetCashBookAsync(DateTime reportDate, CancellationToken cancellationToken = default)
        {
            // Get opening balance (cash transactions before report date)

            var fromLocal = reportDate.Date;
            var fromUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local)
                .ToUniversalTime();

            var toLocalExclusive = fromLocal.AddDays(1);
            var toUtc = DateTime.SpecifyKind(toLocalExclusive, DateTimeKind.Local)
                .ToUniversalTime();

            var dateWithUTCTime = reportDate.GetDateUtcTime();
            var toDate = toUtc > dateWithUTCTime ? toUtc : dateWithUTCTime;


            var lastOpeningBalance = await _transactionRepository.Query()
                .Include(t => t.TransactionHead)
                .Where(t =>
                    t.PaymentMethod == PaymentMethods.CASH &&
                    !t.IsDeleted &&
                    !t.IsArchived &&
                    t.TransactionHead!.UsageFor == UsageFor.OPENING_BALANCE
                     && t.TransactionDate < toDate
                    )
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new
                {
                    t.TransactionDate,
                    t.NetAmount
                })
                .FirstOrDefaultAsync(cancellationToken);


            var openingDate = lastOpeningBalance?.TransactionDate ?? dateWithUTCTime;

            var previousCashAmount = await _transactionRepository.Query()
                .Include(t => t.TransactionHead)
                .Where(t =>
                    t.PaymentMethod == PaymentMethods.CASH &&
                    !t.IsDeleted &&
                    !t.IsArchived &&
                    t.TransactionDate >= openingDate &&
                    t.TransactionDate < fromUtc &&
                    t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
                .SumAsync(t => t.NetAmount, cancellationToken);

            // Calculate opening balance from bank
            var previousBankAmount = await _bankTransactionRepository.Query()
                .Where(bt =>
                    bt.IsActive &&
                    bt.TransactionDate >= openingDate &&
                    bt.TransactionDate < fromUtc)
                .SumAsync(bt => bt.TransactionType == BankTransactionTypes.Deposit ? -bt.Amount : bt.Amount, cancellationToken);

            var openingBalance = (lastOpeningBalance?.NetAmount ?? 0) + previousCashAmount + previousBankAmount;
            // var openingBalance = (lastOpeningBalance?.NetAmount ?? 0) + previousAmount;

            // Get cash transactions for the report date
            var transactions = await _transactionRepository.Query()
                .Include(t => t.TransactionHead)
                .Where(t => t.TransactionDate >= fromUtc && t.TransactionDate < toUtc && t.PaymentMethod == PaymentMethods.CASH && !t.IsDeleted && t.IsArchived == false && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
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

                runningBalance += creditAmount + debitAmount;

                items.Add(new CashBookItemResponse
                {
                    TransactionHeadId = group.TransactionHeadId,
                    TransactionHeadName = group.TransactionHeadName,
                    TransactionType = group.TransactionType,
                    TransactionCount = group.Count,
                    DebitAmount = (-1) * debitAmount,
                    CreditAmount = creditAmount,
                    Balance = runningBalance
                });
            }

            // Get bank transactions
            var bankTransactions = await _bankTransactionRepository.Query()
                .Include(bt => bt.Bank)
                .Where(bt => bt.TransactionDate >= fromUtc && bt.TransactionDate < toUtc && bt.IsActive && !bt.IsArchived)
                .GroupBy(bt => bt.TransactionType)
                .Select(g => new
                {
                    TransactionType = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(bt => g.Key == BankTransactionTypes.Deposit ? -bt.Amount : bt.Amount)
                })
                .ToListAsync(cancellationToken);
            foreach (var bankGroup in bankTransactions)
            {
                var isCredit = bankGroup.TransactionType == BankTransactionTypes.Withdraw;
                var debitAmount = isCredit ? 0 : bankGroup.TotalAmount;
                var creditAmount = isCredit ? bankGroup.TotalAmount : 0;

                runningBalance += creditAmount + debitAmount;

                items.Add(new CashBookItemResponse
                {
                    TransactionHeadId = Guid.Empty,
                    TransactionHeadName = $"Bank Transaction - {bankGroup.TransactionType}",
                    TransactionType = bankGroup.TransactionType.ToString(),
                    TransactionCount = bankGroup.Count,
                    DebitAmount = -1 * debitAmount,
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
