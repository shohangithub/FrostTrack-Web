using Application.Contractors;
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

        public CashBookService(
            IRepository<Transaction, Guid> transactionRepository,
            IRepository<BankTransaction, long> bankTransactionRepository,
            IBalanceCalculatorService balanceCalculatorService)
        {
            _transactionRepository = transactionRepository;
            _bankTransactionRepository = bankTransactionRepository;
            _balanceCalculatorService = balanceCalculatorService;
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

            var openingBalance = await _balanceCalculatorService.GetOpeningBalanceAsync(fromUtc, toDate, true, cancellationToken);

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
                var isMoneyIn = group.TransactionType == TransactionHeadTypes.CREDIT;
                var debitAmount = isMoneyIn ? group.TotalAmount : 0; // Money IN = Debit
                var creditAmount = !isMoneyIn ? group.TotalAmount : 0; // Money OUT = Credit

                runningBalance += debitAmount - creditAmount;

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

            // Get bank transactions
            var bankTransactions = await _bankTransactionRepository.Query()
                .Include(bt => bt.Bank)
                .Where(bt => bt.TransactionDate >= fromUtc && bt.TransactionDate < toUtc && bt.IsActive && !bt.IsArchived)
                .GroupBy(bt => bt.TransactionType)
                .Select(g => new
                {
                    TransactionType = g.Key,
                    Count = g.Count(),
                TotalAmount = g.Sum(bt => bt.Amount)
            })
            .ToListAsync(cancellationToken);
        foreach (var bankGroup in bankTransactions)
        {
            var isDeposit = bankGroup.TransactionType == BankTransactionTypes.Deposit;
            var debitAmount2 = isDeposit ? bankGroup.TotalAmount : 0; // Money IN = Debit
            var creditAmount2 = !isDeposit ? bankGroup.TotalAmount : 0; // Money OUT = Credit

            runningBalance += debitAmount2 - creditAmount2;

                items.Add(new CashBookItemResponse
                {
                    TransactionHeadId = Guid.Empty,
                    TransactionHeadName = $"Bank Transaction - {bankGroup.TransactionType}",
                    TransactionType = bankGroup.TransactionType.ToString(),
                    TransactionCount = bankGroup.Count,
                    DebitAmount = debitAmount2,
                    CreditAmount = creditAmount2,
                    Balance = runningBalance
                });
            }


            var totalDebit = items.Sum(i => i.DebitAmount);
            var totalCredit = items.Sum(i => i.CreditAmount);
            var closingBalance = openingBalance + totalDebit - totalCredit;

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
