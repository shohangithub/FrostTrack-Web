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

        var openingDate = lastOpeningBalance?.TransactionDate ?? DateTime.MinValue;
        var openingBalanceAmount = lastOpeningBalance?.NetAmount ?? 0m;

        // 2. Sum all active transactions between the last opening balance and the report start date (fromUtc)
        var previousCashAmount = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t =>
                t.TenantId == _tenantId &&
                !t.IsDeleted &&
                !t.IsArchived &&
                t.TransactionDate >= openingDate &&
                t.TransactionDate < fromUtc &&
                t.PaymentMethod != PaymentMethods.CREDIT &&
                t.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE &&
                t.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
            // Fix: properly sign the NetAmount based on CREDIT/DEBIT
            .SumAsync(t => t.TransactionHead!.Type == TransactionHeadTypes.DEBIT ? t.NetAmount : -t.NetAmount, cancellationToken);

        return openingBalanceAmount + previousCashAmount;
    }

    public async Task<decimal> GetBankOpeningBalanceAsync(DateTime fromUtc, DateTime toDate, CancellationToken cancellationToken = default)
    {
        // We only sum up to fromUtc because bank doesn't have an explicit OPENING_BALANCE transaction logic 
        // linked to a specific date like cash does in this system (or if it does, it's not implemented yet).
        // Since we are replacing the old logic, we replicate exactly what it did:
        
        // Find the cash opening balance date to bound the bank query, matching legacy logic
        var lastOpeningBalanceDate = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t =>
                t.TenantId == _tenantId &&
                !t.IsDeleted &&
                !t.IsArchived &&
                t.TransactionHead!.UsageFor == UsageFor.OPENING_BALANCE &&
                t.TransactionDate < toDate)
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => (DateTime?)t.TransactionDate)
            .FirstOrDefaultAsync(cancellationToken);

        var openingDate = lastOpeningBalanceDate ?? DateTime.MinValue;

        var previousBankAmount = await _bankTransactionRepository.Query()
            .Where(bt =>
                bt.TenantId == _tenantId &&
                bt.IsActive &&
                bt.TransactionDate >= openingDate &&
                bt.TransactionDate < fromUtc)
            // Bank transaction Deposit = money IN (positive). Withdraw = money OUT (negative).
            // (Note: Legacy CashBook subtracted deposits! That was a bug. Deposit adds to bank balance.)
            .SumAsync(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount, cancellationToken);

        return previousBankAmount;
    }
}
