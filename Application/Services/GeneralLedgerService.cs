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

        // Get cash and bank revenue/expense transactions
        var transactions = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Include(t => t.Bank)
            .Where(t =>
                t.TransactionDate >= fromUtc &&
                t.TransactionDate < toUtc &&
                !t.IsDeleted &&
                !t.IsArchived &&
                t.PaymentMethod != PaymentMethods.CREDIT &&
                t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE &&
                t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
            .OrderBy(t => t.CreatedTime)
            .ToListAsync(cancellationToken);

        var items = new List<GeneralLedgerItemResponse>();
        decimal totalDebit = 0;
        decimal totalCredit = 0;

        // Add transactions
        foreach (var transaction in transactions)
        {
            // Determine if the transaction is Money IN (DEBIT) or Money OUT (CREDIT)
            var isMoneyIn = transaction.TransactionHead?.Type == TransactionHeadTypes.DEBIT;
            var debitAmount = isMoneyIn ? transaction.NetAmount : 0; // Money IN = Debit
            var creditAmount = !isMoneyIn ? transaction.NetAmount : 0; // Money OUT = Credit

            totalDebit += debitAmount;
            totalCredit += creditAmount;

            var isBankOrCheque = transaction.PaymentMethod != PaymentMethods.CASH || transaction.BankId.HasValue;
            var accountName = transaction.TransactionHead?.Name ?? "Unknown";
            if (transaction.Bank != null)
            {
                accountName = $"{accountName} ({transaction.Bank.BankName})";
            }

            var description = transaction.Description ?? string.Empty;
            if (isBankOrCheque)
            {
                var extraParts = new List<string>();
                if (transaction.Bank != null && !description.Contains(transaction.Bank.BankName))
                    extraParts.Add(transaction.Bank.BankName);
                if (!string.IsNullOrEmpty(transaction.PaymentMethod) && transaction.PaymentMethod != PaymentMethods.CASH && !description.Contains(transaction.PaymentMethod))
                    extraParts.Add(transaction.PaymentMethod);
                if (!string.IsNullOrEmpty(transaction.PaymentReference) && !description.Contains(transaction.PaymentReference))
                    extraParts.Add($"Ref: {transaction.PaymentReference}");

                if (extraParts.Any())
                {
                    description = $"{description} [{string.Join(" - ", extraParts)}]";
                }
            }

            items.Add(new GeneralLedgerItemResponse
            {
                Id = transaction.Id.ToString(),
                Date = transaction.TransactionDate,
                TransactionCode = transaction.TransactionCode,
                Description = description,
                AccountName = accountName,
                AccountType = isBankOrCheque ? "Bank" : "Cash",
                TransactionType = transaction.TransactionHead?.Type ?? "Unknown",
                PaymentMethod = transaction.PaymentMethod,
                ReferenceNo = transaction.PaymentReference,
                DebitAmount = debitAmount,
                CreditAmount = creditAmount
            });
        }

        // Get bank transactions (exclude auto-created bill collections to avoid double-counting with 'transactions' above)
        var bankTransactions = await _bankTransactionRepository.Query()
            .Include(bt => bt.Bank)
            .Where(bt => bt.TransactionDate >= fromUtc && bt.TransactionDate < toUtc && bt.IsActive && !bt.IsDeleted
                && bt.SourceType != BankSourceTypes.BILL_COLLECTION
                && bt.TransactionId == null)
            .OrderBy(bt => bt.CreatedTime)
            .ToListAsync(cancellationToken);

        // Add bank transactions
        foreach (var bankTransaction in bankTransactions)
        {
            var isDeposit = bankTransaction.TransactionType == BankTransactionTypes.Deposit;
            var isCashContra = bankTransaction.SourceType == BankSourceTypes.CASH || string.IsNullOrEmpty(bankTransaction.SourceType);

            if (isCashContra)
            {
                // Internal Transfer (Contra): Money moved between Cash and Bank
                // Leg 1: The Bank side
                var bankDebit = isDeposit ? bankTransaction.Amount : 0;
                var bankCredit = !isDeposit ? bankTransaction.Amount : 0;

                totalDebit += bankDebit;
                totalCredit += bankCredit;

                items.Add(new GeneralLedgerItemResponse
                {
                    Id = $"{bankTransaction.Id}-BANK",
                    Date = bankTransaction.TransactionDate,
                    TransactionCode = $"BANK-{bankTransaction.Id}",
                    Description = isDeposit 
                        ? $"Bank Deposit from Cash ({bankTransaction.Bank?.BankName})" 
                        : $"Bank Withdrawal to Cash ({bankTransaction.Bank?.BankName})",
                    AccountName = $"{bankTransaction.Bank?.BankName} - {bankTransaction.Bank?.AccountNumber}",
                    AccountType = "Bank",
                    TransactionType = "Contra",
                    PaymentMethod = "Bank",
                    ReferenceNo = bankTransaction.Reference,
                    DebitAmount = bankDebit,
                    CreditAmount = bankCredit
                });

                // Leg 2: The Cash side (balancing contra entry so total company fund remains accurate)
                var cashDebit = !isDeposit ? bankTransaction.Amount : 0;
                var cashCredit = isDeposit ? bankTransaction.Amount : 0;

                totalDebit += cashDebit;
                totalCredit += cashCredit;

                items.Add(new GeneralLedgerItemResponse
                {
                    Id = $"{bankTransaction.Id}-CASH",
                    Date = bankTransaction.TransactionDate,
                    TransactionCode = $"CASH-{bankTransaction.Id}",
                    Description = isDeposit 
                        ? $"Cash transferred to Bank ({bankTransaction.Bank?.BankName})" 
                        : $"Cash received from Bank ({bankTransaction.Bank?.BankName})",
                    AccountName = "Cash in Hand (নগদ তহবিল)",
                    AccountType = "Cash",
                    TransactionType = "Contra",
                    PaymentMethod = "Cash",
                    ReferenceNo = bankTransaction.Reference,
                    DebitAmount = cashDebit,
                    CreditAmount = cashCredit
                });
            }
            else
            {
                // External Bank Transaction (Interest, Direct Wire, Capital, Loan)
                var debitAmount = isDeposit ? bankTransaction.Amount : 0;
                var creditAmount = !isDeposit ? bankTransaction.Amount : 0;

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
