using Application.Contractors;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class GeneralLedgerService : IGeneralLedgerService
{
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<BankTransaction, long> _bankTransactionRepository;

    public GeneralLedgerService(
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<BankTransaction, long> bankTransactionRepository)
    {
        _transactionRepository = transactionRepository;
        _bankTransactionRepository = bankTransactionRepository;
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

        // Get last opening balance
        var lastOpeningBalance = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t =>
                !t.IsArchived &&
                t.TransactionHead!.UsageFor == UsageFor.OPENING_BALANCE &&
                t.TransactionDate < dateWithUTCTime)
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new
            {
                t.TransactionDate,
                t.NetAmount
            })
            .FirstOrDefaultAsync(cancellationToken);

        var openingDate = lastOpeningBalance?.TransactionDate ?? dateWithUTCTime;

        // Calculate opening balance from cash
        var previousCashAmount = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t =>
                !t.IsArchived &&
                t.TransactionDate >= openingDate &&
                t.TransactionDate < fromUtc &&
                t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE &&
                t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
            .SumAsync(t => t.NetAmount, cancellationToken);

        // Calculate opening balance from bank
        var previousBankAmount = await _bankTransactionRepository.Query()
            .Where(bt =>
                bt.IsActive &&
                bt.TransactionDate >= openingDate &&
                bt.TransactionDate < fromUtc)
            .SumAsync(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount, cancellationToken);

        var openingBalance = (lastOpeningBalance?.NetAmount ?? 0) + previousCashAmount + previousBankAmount;

        // Get cash transactions
        var transactions = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t =>
                t.TransactionDate >= fromUtc &&
                t.TransactionDate < toUtc &&
                !t.IsArchived &&
                t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE &&
                t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
            .OrderBy(t => t.CreatedTime)
            .ToListAsync(cancellationToken);

        // Get bank transactions
        var bankTransactions = await _bankTransactionRepository.Query()
            .Include(bt => bt.Bank)
            .Where(bt => bt.TransactionDate >= fromUtc && bt.TransactionDate < toUtc && bt.IsActive)
            .OrderBy(bt => bt.CreatedTime)
            .ToListAsync(cancellationToken);

        var items = new List<GeneralLedgerItemResponse>();
        decimal totalDebit = 0;
        decimal totalCredit = 0;

        // Add cash transactions
        foreach (var transaction in transactions)
        {
            var isCredit = transaction.TransactionHead?.Type == TransactionHeadTypes.CREDIT;
            var debitAmount = isCredit ? 0 : transaction.NetAmount;
            var creditAmount = isCredit ? transaction.NetAmount : 0;

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
                DebitAmount = (-1) * debitAmount,
                CreditAmount = creditAmount
            });
        }

        // Add bank transactions
        foreach (var bankTransaction in bankTransactions)
        {
            var isCredit = bankTransaction.TransactionType == BankTransactionTypes.Deposit;
            var debitAmount = isCredit ? 0 : bankTransaction.Amount;
            var creditAmount = isCredit ? bankTransaction.Amount : 0;

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

        var closingBalance = openingBalance + totalCredit + totalDebit;

        return new GeneralLedgerResponse
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
