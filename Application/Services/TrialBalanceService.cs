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
        DateTime startDate,
        DateTime endDate,
        int? branchId,
        CancellationToken cancellationToken)
    {
        // Fetch cash transactions
        var transactionQuery = _transactionRepository.Query()
            .Where(t => t.TenantId == _tenantId
                     && t.TransactionDate >= startDate
                     && t.TransactionDate <= endDate
                     && !t.IsDeleted);

        if (branchId.HasValue)
        {
            transactionQuery = transactionQuery.Where(t => t.BranchId == branchId.Value);
        }

        var transactions = await transactionQuery.ToListAsync(cancellationToken);

        // Fetch bank transactions
        var bankTransactionQuery = _bankTransactionRepository.Query()
            .Include(bt => bt.Bank)
            .Where(bt => bt.TenantId == _tenantId
                      && bt.TransactionDate >= startDate
                      && bt.TransactionDate <= endDate
                      && bt.IsActive);

        if (branchId.HasValue)
        {
            bankTransactionQuery = bankTransactionQuery.Where(bt => bt.BranchId == branchId.Value);
        }

        var bankTransactions = await bankTransactionQuery.ToListAsync(cancellationToken);

        // Group cash transactions by type and calculate debits/credits
        var groupedTransactions = transactions
            .GroupBy(t => new { t.TransactionType, t.Category })
            .Select(g => new TrialBalanceItemResponse
            {
                AccountName = string.IsNullOrEmpty(g.Key.Category)
                    ? GetTransactionTypeName(g.Key.TransactionType)
                    : g.Key.Category,
                AccountType = g.Key.TransactionType,
                DebitAmount = g.Where(t => t.TransactionFlow == "OUT")
                              .Sum(t => t.NetAmount),
                CreditAmount = g.Where(t => t.TransactionFlow == "IN")
                               .Sum(t => t.NetAmount),
                TransactionCount = g.Count(),
                Balance = g.Where(t => t.TransactionFlow == "IN")
                           .Sum(t => t.NetAmount) -
                          g.Where(t => t.TransactionFlow == "OUT")
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
                DebitAmount = g.Where(bt => bt.TransactionType == "Withdraw")
                              .Sum(bt => bt.Amount),
                CreditAmount = g.Where(bt => bt.TransactionType == "Deposit")
                               .Sum(bt => bt.Amount),
                TransactionCount = g.Count(),
                Balance = g.Where(bt => bt.TransactionType == "Deposit")
                           .Sum(bt => bt.Amount) -
                          g.Where(bt => bt.TransactionType == "Withdraw")
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

        return new TrialBalanceSummaryResponse
        {
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            NetBalance = totalCredit - totalDebit,
            StartDate = startDate,
            EndDate = endDate,
            TotalTransactions = totalTransactionCount,
            Items = allItems
        };
    }

    private static string GetTransactionTypeName(string transactionType)
    {
        return transactionType switch
        {
            "BILL_COLLECTION" => "Bill Collection",
            "OFFICE_COST" => "Office Cost",
            "BILL_PAYMENT" => "Bill Payment",
            "ADJUSTMENT" => "Adjustment",
            "REFUND" => "Refund",
            "SALARY" => "Salary Payment",
            "OTHER" => "Other",
            _ => transactionType
        };
    }
}
