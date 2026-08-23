using Application.Contractors;
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

    public GeneralLedgerService(
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<BankTransaction, long> bankTransactionRepository,
        IBalanceCalculatorService balanceCalculatorService)
    {
        _transactionRepository = transactionRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _balanceCalculatorService = balanceCalculatorService;
    }

    public async Task<GeneralLedgerResponse> GetGeneralLedgerAsync(DateTime reportDate, CancellationToken cancellationToken = default)
    {
        var fromLocal = reportDate.Date;
        var fromUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local)
            .ToUniversalTime();

        var toLocalExclusive = fromLocal.AddDays(1);
        var toUtc = DateTime.SpecifyKind(toLocalExclusive, DateTimeKind.Local)
            .ToUniversalTime();

        var dateWithUTCTime = reportDate.GetDateUtcTime();
        var toDate = toUtc > dateWithUTCTime ? toUtc : dateWithUTCTime;

        var openingBalance = await _balanceCalculatorService.GetOpeningBalanceAsync(fromUtc, toDate, true, cancellationToken);

        // Get cash transactions
        var transactions = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t =>
                t.TransactionDate >= fromUtc &&
                t.TransactionDate < toUtc &&
                !t.IsDeleted &&
                !t.IsArchived &&
                t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE &&
                t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
            .OrderBy(t => t.CreatedTime)
            .ToListAsync(cancellationToken);

        var items = new List<GeneralLedgerItemResponse>();
        decimal totalDebit = 0;
        decimal totalCredit = 0;

        // Add cash transactions
        foreach (var transaction in transactions)
        {
            var isMoneyIn = transaction.TransactionHead?.Type == TransactionHeadTypes.CREDIT;
            var debitAmount = isMoneyIn ? transaction.NetAmount : 0; // Money IN = Debit
            var creditAmount = !isMoneyIn ? transaction.NetAmount : 0; // Money OUT = Credit

            totalDebit += debitAmount;
            totalCredit += creditAmount;

            items.Add(new GeneralLedgerItemResponse
            {
                Id = transaction.Id.ToString(),
                Date = transaction.TransactionDate,
                TransactionCode = transaction.TransactionCode,
                Description = transaction.Description,
                AccountName = transaction.TransactionHead?.Name ?? "Unknown",
                AccountType = "Cash",
                TransactionType = transaction.TransactionHead?.Type ?? "Unknown",
                PaymentMethod = transaction.PaymentMethod,
                ReferenceNo = transaction.PaymentReference,
                DebitAmount = debitAmount,
                CreditAmount = creditAmount
            });
        }


        // Get bank transactions
        var bankTransactions = await _bankTransactionRepository.Query()
            .Include(bt => bt.Bank)
            .Where(bt => bt.TransactionDate >= fromUtc && bt.TransactionDate < toUtc && bt.IsActive)
            .OrderBy(bt => bt.CreatedTime)
            .ToListAsync(cancellationToken);
        // Add bank transactions
        foreach (var bankTransaction in bankTransactions)
        {
            // Bank Deposit = money coming IN (DEBIT side), Withdrawal = money going OUT (CREDIT side)
            var isDeposit = bankTransaction.TransactionType == BankTransactionTypes.Deposit;
            var debitAmount = isDeposit ? bankTransaction.Amount : 0; // Money IN = Debit
            var creditAmount = !isDeposit ? bankTransaction.Amount : 0; // Money OUT = Credit

            totalDebit += debitAmount;
            totalCredit += creditAmount;

            items.Add(new GeneralLedgerItemResponse
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

        // Sort all items by date
        items = items.OrderBy(i => i.Date).ToList();

        var closingBalance = openingBalance + totalDebit - totalCredit;

        return new GeneralLedgerResponse
        {
            ReportDate = reportDate,
            OpeningBalance = openingBalance,
            Items = items,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            ClosingBalance = closingBalance
        };
    }
}
