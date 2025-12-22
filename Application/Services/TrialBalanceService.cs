using Application.Contractors;
using Application.Contractors.Authentication;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class TrialBalanceService : ITrialBalanceService
{
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<BankTransaction, long> _bankTransactionRepository;
    private readonly Guid _tenantId;

    public TrialBalanceService(
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<BankTransaction, long> bankTransactionRepository,
        ITenantProvider tenantProvider)
    {
        _transactionRepository = transactionRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _tenantId = tenantProvider.GetTenantId();
    }

    public async Task<TrialBalanceSummaryResponse> GetTrialBalanceAsync(
        DateTime reportDate,
        CancellationToken cancellationToken)
    {
        // Get opening balance (all transactions before reportDate)
        var openingBalanceQuery = _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t => t.TenantId == _tenantId
                     && t.TransactionDate.Date < reportDate.Date
                     && !t.IsDeleted && t.IsArchived == false);

        var openingTransactions = await openingBalanceQuery.ToListAsync(cancellationToken);
        var openingBalance = openingTransactions
            .Where(t => t.TransactionHead!.Type == TransactionHeadTypes.CREDIT && !t.IsArchived)
            .Sum(t => t.NetAmount) - openingTransactions
            .Where(t => t.TransactionHead!.Type == TransactionHeadTypes.DEBIT && !t.IsArchived)
            .Sum(t => t.NetAmount);

        // Fetch cash transactions for the report date
        var transactionQuery = _transactionRepository.Query().Include(t => t.TransactionHead)
            .Where(t => t.TenantId == _tenantId
                     && t.TransactionDate.Date == reportDate.Date
                     && !t.IsDeleted);

        var transactions = await transactionQuery.ToListAsync(cancellationToken);

        // Fetch bank transactions for the report date
        var bankTransactionQuery = _bankTransactionRepository.Query()
            .Include(bt => bt.Bank)
            .Where(bt => bt.TenantId == _tenantId
                      && bt.TransactionDate.Date == reportDate.Date
                      && bt.IsActive);

        var bankTransactions = await bankTransactionQuery.ToListAsync(cancellationToken);

        // Group cash transactions by type and calculate debits/credits
        var groupedTransactions = transactions
            .GroupBy(t => new { t.TransactionHead!.Name })
            .Select(g => new TrialBalanceItemResponse
            {
                AccountName = g.Key.Name,
                AccountType = g.Key.Name,
                DebitAmount = (-1) * g.Where(t => t.TransactionHead!.Type == TransactionHeadTypes.DEBIT && !t.IsArchived)
                              .Sum(t => t.NetAmount),
                CreditAmount = g.Where(t => t.TransactionHead!.Type == TransactionHeadTypes.CREDIT && !t.IsArchived)
                               .Sum(t => t.NetAmount),
                TransactionCount = g.Count(),
                Balance = g.Where(t => t.TransactionHead!.Type == TransactionHeadTypes.CREDIT && !t.IsArchived)
                           .Sum(t => t.NetAmount) -
                          g.Where(t => t.TransactionHead!.Type == TransactionHeadTypes.DEBIT && !t.IsArchived)
                           .Sum(t => t.NetAmount)
            })
            .ToList();

        // Group bank transactions by bank and transaction type
        var groupedBankTransactions = bankTransactions
            .GroupBy(bt => new { bt.Bank.BankName, bt.TransactionType })
            .Select(g => new TrialBalanceItemResponse
            {
                AccountName = $"{g.Key.BankName} - {g.Key.TransactionType}",
                AccountType = "BANK_TRANSACTION",
                DebitAmount = g.Where(bt => bt.TransactionType == "Withdraw" && bt.IsActive)
                              .Sum(bt => bt.Amount),
                CreditAmount = g.Where(bt => bt.TransactionType == "Deposit" && bt.IsActive)
                               .Sum(bt => bt.Amount),
                TransactionCount = g.Count(),
                Balance = g.Where(bt => bt.TransactionType == "Deposit" && bt.IsActive)
                           .Sum(bt => bt.Amount) -
                          g.Where(bt => bt.TransactionType == "Withdraw" && bt.IsActive)
                           .Sum(bt => bt.Amount)
            })
            .ToList();

        // Combine both lists
        var allItems = groupedTransactions.Concat(groupedBankTransactions)
            .OrderBy(item => item.AccountName)
            .ToList();

        var totalDebit = allItems.Sum(t => t.DebitAmount);
        var totalCredit = allItems.Sum(t => t.CreditAmount);
        var totalTransactionCount = transactions.Count + bankTransactions.Count;
        var closingBalance = openingBalance + totalCredit - totalDebit;

        return new TrialBalanceSummaryResponse
        {
            ReportDate = reportDate,
            OpeningBalance = openingBalance,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            ClosingBalance = closingBalance,
            TotalTransactions = totalTransactionCount,
            Items = allItems
        };
    }
}
