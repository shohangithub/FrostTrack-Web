using Application.Contractors;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class BankBookService : IBankBookService
{
    private readonly IRepository<BankTransaction, long> _bankTransactionRepository;
    private readonly IRepository<Bank, int> _bankRepository;

    public BankBookService(IRepository<BankTransaction, long> bankTransactionRepository, IRepository<Bank, int> bankRepository)
    {
        _bankTransactionRepository = bankTransactionRepository;
        _bankRepository = bankRepository;
    }

    public async Task<BankBookResponse> GetBankBookAsync(DateTime reportDate, CancellationToken cancellationToken = default)
    {
        var fromLocal = reportDate.Date;
        var fromUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local)
            .ToUniversalTime();

        var toLocalExclusive = fromLocal.AddDays(1);
        var toUtc = DateTime.SpecifyKind(toLocalExclusive, DateTimeKind.Local)
            .ToUniversalTime();

        // Calculate opening balance (all active bank initial opening balances + all bank transactions before report date)
        var bankInitialOpening = await _bankRepository.Query()
            .Where(b => b.IsActive && !b.IsDeleted)
            .SumAsync(b => b.OpeningBalance, cancellationToken);

        var openingBalance = bankInitialOpening + await _bankTransactionRepository.Query()
            .Where(bt => bt.IsActive && !bt.IsDeleted && !bt.IsArchived && bt.TransactionDate < fromUtc)
            .SumAsync(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount, cancellationToken);

        // Get bank transactions for the report date
        var bankTransactions = await _bankTransactionRepository.Query()
            .Include(bt => bt.Bank)
            .Where(bt => bt.TransactionDate >= fromUtc && bt.TransactionDate < toUtc && bt.IsActive && !bt.IsDeleted && !bt.IsArchived)
            .OrderBy(bt => bt.CreatedTime)
            .ToListAsync(cancellationToken);

        var items = new List<BankBookItemResponse>();
        decimal runningBalance = openingBalance;
        decimal totalDebit = 0;
        decimal totalCredit = 0;

        foreach (var bankTransaction in bankTransactions)
        {
            var isDeposit = bankTransaction.TransactionType == BankTransactionTypes.Deposit;
            var debitAmount = isDeposit ? bankTransaction.Amount : 0; // Money IN = Debit
            var creditAmount = !isDeposit ? bankTransaction.Amount : 0; // Money OUT = Credit

            totalDebit += debitAmount;
            totalCredit += creditAmount;
            runningBalance += debitAmount - creditAmount;

            items.Add(new BankBookItemResponse
            {
                Id = bankTransaction.Id,
                Date = bankTransaction.TransactionDate,
                TransactionCode = $"BANK-{bankTransaction.Id}",
                Description = bankTransaction.Description ?? "Bank Transaction",
                BankName = bankTransaction.Bank?.BankName ?? "Unknown Bank",
                AccountNumber = bankTransaction.Bank?.AccountNumber ?? "",
                TransactionType = bankTransaction.TransactionType,
                ReferenceNo = bankTransaction.Reference,
                DebitAmount = debitAmount,
                CreditAmount = creditAmount,
                Balance = runningBalance
            });
        }

        var closingBalance = openingBalance + totalDebit - totalCredit;

        return new BankBookResponse
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
