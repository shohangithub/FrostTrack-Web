using Application.Contractors;
using Application.Contractors.Authentication;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Common;

public class BalanceCalculatorService : IBalanceCalculatorService
{
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<BankTransaction, long> _bankTransactionRepository;
    private readonly Guid _tenantId;

    public BalanceCalculatorService(
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<BankTransaction, long> bankTransactionRepository,
        ITenantProvider tenantProvider)
    {
        _transactionRepository = transactionRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _tenantId = tenantProvider.GetTenantId();
    }

    public async Task<decimal> GetOpeningBalanceAsync(DateTime fromUtc, DateTime toDate, bool includeBank, CancellationToken cancellationToken = default)
    {
        var cashBalance = await GetCashOpeningBalanceAsync(fromUtc, toDate, cancellationToken);
        
        if (includeBank)
        {
            var bankBalance = await GetBankOpeningBalanceAsync(fromUtc, toDate, cancellationToken);
            return cashBalance + bankBalance;
        }

        return cashBalance;
    }

    public async Task<decimal> GetCashOpeningBalanceAsync(DateTime fromUtc, DateTime toDate, CancellationToken cancellationToken = default)
    {
        // 1. Find the latest explicitly entered opening balance before toDate
        var lastOpeningBalance = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t =>
                t.TenantId == _tenantId &&
                !t.IsDeleted &&
                !t.IsArchived &&
                t.TransactionHead!.UsageFor == UsageFor.OPENING_BALANCE &&
                t.TransactionDate < toDate)
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new
            {
                t.TransactionDate,
                // CREDIT is positive (money in), DEBIT is negative (money out)
                NetAmount = t.TransactionHead!.Type == TransactionHeadTypes.DEBIT ? t.NetAmount : -t.NetAmount
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Normalize opening balance date to the start of that day (00:00:00 local) in UTC,
        // so any transactions recorded on that day (regardless of timestamp) are properly counted.
        var openingDate = lastOpeningBalance != null
            ? DateTime.SpecifyKind(lastOpeningBalance.TransactionDate.ToLocalTime().Date, DateTimeKind.Local).ToUniversalTime()
            : DateTime.MinValue;
        var openingBalanceAmount = lastOpeningBalance?.NetAmount ?? 0m;

        // 2. Sum all active cash transactions between the last opening balance day and the report start date (fromUtc)
        var previousCashAmount = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t =>
                t.TenantId == _tenantId &&
                !t.IsDeleted &&
                !t.IsArchived &&
                t.TransactionDate >= openingDate &&
                t.TransactionDate < fromUtc &&
                t.PaymentMethod == PaymentMethods.CASH &&
                t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE &&
                t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE &&
                t.TransactionHead!.UsageFor != UsageFor.LABOUR_CHARGE)
            .SumAsync(t => t.TransactionHead!.Type == TransactionHeadTypes.DEBIT ? Math.Abs(t.NetAmount) : -Math.Abs(t.NetAmount), cancellationToken);

        // 3. Account for internal cash-to-bank deposits (cash outflow) and bank-to-cash withdrawals (cash inflow)
        var previousBankCashTransfers = await _bankTransactionRepository.Query()
            .Where(bt =>
                bt.TenantId == _tenantId &&
                bt.IsActive &&
                !bt.IsDeleted &&
                bt.TransactionDate >= openingDate &&
                bt.TransactionDate < fromUtc &&
                (bt.SourceType == null || bt.SourceType == BankSourceTypes.CASH))
            .SumAsync(bt => bt.TransactionType == BankTransactionTypes.Withdraw ? bt.Amount : -bt.Amount, cancellationToken);

        return openingBalanceAmount + previousCashAmount + previousBankCashTransfers;
    }

    public async Task<decimal> GetBankOpeningBalanceAsync(DateTime fromUtc, DateTime toDate, CancellationToken cancellationToken = default)
    {
        // Calculate bank opening balance as all active bank transactions prior to the report date (matching BankBook logic).
        // Bank deposits = money IN (+), Withdrawals = money OUT (-).
        var previousBankAmount = await _bankTransactionRepository.Query()
            .Where(bt =>
                bt.TenantId == _tenantId &&
                bt.IsActive &&
                !bt.IsDeleted &&
                bt.TransactionDate < fromUtc)
            .SumAsync(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount, cancellationToken);

        return previousBankAmount;
    }
}
