using Application.Contractors;
using Application.Contractors.Authentication;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;
using Application.Services.Common;

namespace Application.Services;

public class BankBookService : IBankBookService
{
    private readonly IRepository<BankTransaction, long> _bankTransactionRepository;
    private readonly IRepository<Bank, int> _bankRepository;
    private readonly Guid _tenantId;

    public BankBookService(
        IRepository<BankTransaction, long> bankTransactionRepository,
        IRepository<Bank, int> bankRepository,
        ITenantProvider tenantProvider)
    {
        _bankTransactionRepository = bankTransactionRepository;
        _bankRepository = bankRepository;
        _tenantId = tenantProvider.GetTenantId();
    }

    /// <summary>
    /// Daily Bank Book. Internally a one-day range so daily and range figures always agree.
    /// </summary>
    public async Task<BankBookResponse> GetBankBookAsync(DateTime reportDate, int? bankId = null, CancellationToken cancellationToken = default)
    {
        var result = await BuildAsync(ReportDateWindow.ForDay(reportDate), bankId, cancellationToken);

        return new BankBookResponse
        {
            ReportDate = reportDate,
            OpeningBalance = result.OpeningBalance,
            Items = result.Days.SelectMany(d => d.Items).ToList(),
            TotalDebit = result.TotalDebit,
            TotalCredit = result.TotalCredit,
            ClosingBalance = result.ClosingBalance
        };
    }

    public Task<BankBookRangeResponse> GetBankBookRangeAsync(DateTime fromDate, DateTime toDate, int? bankId = null, CancellationToken cancellationToken = default)
    {
        return BuildAsync(ReportDateWindow.ForRange(fromDate, toDate), bankId, cancellationToken);
    }

    private async Task<BankBookRangeResponse> BuildAsync(ReportDateWindow window, int? bankId, CancellationToken cancellationToken)
    {
        var filterByBank = bankId.HasValue && bankId.Value > 0;

        // Opening balance = active bank initial opening balance(s) + bank transactions before the window
        var bankQuery = _bankRepository.Query()
            .AsNoTracking()
            .Where(b => b.TenantId == _tenantId && b.IsActive && !b.IsDeleted);

        if (filterByBank)
        {
            bankQuery = bankQuery.Where(b => b.Id == bankId!.Value);
        }

        var bankInitialOpening = await bankQuery.SumAsync(b => b.OpeningBalance, cancellationToken);

        string? bankName = null;
        string? accountNumber = null;
        if (filterByBank)
        {
            var bank = await bankQuery
                .Select(b => new { b.BankName, b.AccountNumber })
                .FirstOrDefaultAsync(cancellationToken);
            bankName = bank?.BankName;
            accountNumber = bank?.AccountNumber;
        }

        var prevTxQuery = _bankTransactionRepository.Query()
            .AsNoTracking()
            .Where(bt => bt.TenantId == _tenantId && bt.IsActive && !bt.IsDeleted && !bt.IsArchived
                      && bt.TransactionDate < window.FromUtc);

        if (filterByBank)
        {
            prevTxQuery = prevTxQuery.Where(bt => bt.BankId == bankId!.Value);
        }

        var prevBankTxSum = await prevTxQuery
            .SumAsync(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount, cancellationToken);

        var openingBalance = bankInitialOpening + prevBankTxSum;

        // Bank transactions in the window
        var txQuery = _bankTransactionRepository.Query()
            .AsNoTracking()
            .Include(bt => bt.Bank)
            .Where(bt => bt.TenantId == _tenantId
                      && bt.TransactionDate >= window.FromUtc && bt.TransactionDate < window.ToUtc
                      && bt.IsActive && !bt.IsDeleted && !bt.IsArchived);

        if (filterByBank)
        {
            txQuery = txQuery.Where(bt => bt.BankId == bankId!.Value);
        }

        var bankTransactions = await txQuery
            .OrderBy(bt => bt.TransactionDate)
            .ThenBy(bt => bt.CreatedTime)
            .ToListAsync(cancellationToken);

        var days = new List<BankBookDayResponse>();
        var runningBalance = openingBalance;

        foreach (var dayGroup in bankTransactions.GroupBy(bt => ReportDateWindow.ToLocalDay(bt.TransactionDate)).OrderBy(g => g.Key))
        {
            var day = new BankBookDayResponse
            {
                Date = dayGroup.Key,
                OpeningBalance = runningBalance
            };

            foreach (var bankTransaction in dayGroup)
            {
                var isDeposit = bankTransaction.TransactionType == BankTransactionTypes.Deposit;
                var debitAmount = isDeposit ? bankTransaction.Amount : 0;   // Money IN = Debit
                var creditAmount = !isDeposit ? bankTransaction.Amount : 0; // Money OUT = Credit

                day.TotalDebit += debitAmount;
                day.TotalCredit += creditAmount;
                runningBalance += debitAmount - creditAmount;

                day.Items.Add(new BankBookItemResponse
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

            day.ClosingBalance = runningBalance;
            days.Add(day);
        }

        return new BankBookRangeResponse
        {
            FromDate = window.FromDate,
            ToDate = window.ToDate,
            BankId = filterByBank ? bankId : null,
            BankName = bankName,
            AccountNumber = accountNumber,
            OpeningBalance = openingBalance,
            Days = days,
            TotalDebit = days.Sum(d => d.TotalDebit),
            TotalCredit = days.Sum(d => d.TotalCredit),
            ClosingBalance = runningBalance
        };
    }
}
