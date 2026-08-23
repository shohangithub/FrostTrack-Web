using Application.Contractors;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;
using Application.Services.Common;

namespace Application.Services
{
    public class LedgerBookService : ILedgerBookService
    {
        private readonly IRepository<Transaction, Guid> _transactionRepository;
        private readonly IBalanceCalculatorService _balanceCalculatorService;

        public LedgerBookService(
            IRepository<Transaction, Guid> transactionRepository,
            IBalanceCalculatorService balanceCalculatorService)
        {
            _transactionRepository = transactionRepository;
            _balanceCalculatorService = balanceCalculatorService;
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
            var toDate = toUtc > dateWithUTCTime ? toUtc : dateWithUTCTime;

            // Ledger book traditionally only includes cash transactions in this system
            var openingBalance = await _balanceCalculatorService.GetOpeningBalanceAsync(fromUtc, toDate, false, cancellationToken);

            // Get transactions for the report date
            var transactions = await _transactionRepository.Query()
                .Include(t => t.TransactionHead)
                .Where(t => t.TransactionDate >= fromUtc && t.TransactionDate < toUtc && !t.IsDeleted && !t.IsArchived && t.PaymentMethod != PaymentMethods.CREDIT && t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE && t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
                .OrderBy(t => t.CreatedTime)
                .ToListAsync(cancellationToken);

            var items = new List<LedgerBookItemResponse>();
            decimal totalDebit = 0;
            decimal totalCredit = 0;

            foreach (var transaction in transactions)
            {
                var isMoneyIn = transaction.TransactionHead?.Type == TransactionHeadTypes.DEBIT;
                var debitAmount = isMoneyIn ? transaction.NetAmount : 0; // Money IN = Debit
                var creditAmount = !isMoneyIn ? transaction.NetAmount : 0; // Money OUT = Credit

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
                    DebitAmount = debitAmount,
                    CreditAmount = creditAmount,
                    Balance = 0 // Not needed for general ledger
                });
            }

            var closingBalance = openingBalance + totalDebit - totalCredit;

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
