using Application.RequestDTO;
using Application.Services.Common;
using Mapster;

public class SupplierPaymentRepository : ISupplierPaymentRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly IRepository<SupplierPayment, long> _repository;

    public SupplierPaymentRepository(ApplicationDbContext context, DefaultValueInjector defaultValueInjector, IRepository<SupplierPayment, long> repository)
    {
        _context = context;
        _defaultValueInjector = defaultValueInjector;
        _repository = repository;
    }

    public async Task<SupplierPayment?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.SupplierPayments
            .Include(p => p.Supplier)
            .Include(p => p.Customer)
            .Include(p => p.Bank)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public IQueryable<SupplierPayment> Query()
    {
        return _context.SupplierPayments
            .Include(x => x.Supplier)
            .Include(x => x.Customer)
            .Include(x => x.Bank)
            .AsNoTracking();
    }

    public async Task<SupplierPaymentResponse> ManageUpdate(SupplierPaymentRequest request, SupplierPayment existingData, CancellationToken cancellationToken = default)
    {
        // Simple payment tracking without detailed invoice breakdown
        // The payment amount represents the total payment to the supplier

        // Handle new vs existing payment
        if (existingData.Id == 0)
        {
            // New payment
            _defaultValueInjector.InjectCreatingAudit<SupplierPayment, long>(existingData);
            _context.Entry(existingData).State = EntityState.Added;
        }
        else
        {
            // Existing payment - update the existing entity
            existingData.PaymentNumber = request.PaymentNumber;
            existingData.PaymentDate = request.PaymentDate;
            existingData.PaymentType = request.PaymentType;
            existingData.SupplierId = request.SupplierId;
            existingData.CustomerId = request.CustomerId;
            existingData.PaymentMethod = request.PaymentMethod;
            existingData.BankId = request.BankId;
            existingData.CheckNumber = request.CheckNumber;
            existingData.CheckDate = request.CheckDate;
            existingData.OnlinePaymentMethod = request.OnlinePaymentMethod;
            existingData.TransactionId = request.TransactionId;
            existingData.GatewayReference = request.GatewayReference;
            existingData.MobileWalletType = request.MobileWalletType;
            existingData.WalletNumber = request.WalletNumber;
            existingData.WalletTransactionId = request.WalletTransactionId;
            existingData.CardType = request.CardType;
            existingData.CardLastFour = request.CardLastFour;
            existingData.CardTransactionId = request.CardTransactionId;
            existingData.PaymentAmount = request.PaymentAmount;
            existingData.Notes = request.Notes;
            existingData.BranchId = request.BranchId;

            _defaultValueInjector.InjectUpdatingAudit<SupplierPayment, long>(existingData);
            _context.Entry(existingData).State = EntityState.Modified;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var result = await GetByIdAsync(existingData.Id, cancellationToken);
        return result!.Adapt<SupplierPaymentResponse>();
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var payment = await GetByIdAsync(id, cancellationToken);
        if (payment == null) return false;

        // Simple deletion without complex detail reversal
        var result = await _repository.DeletableQuery(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
        return result > 0;
    }

    public async Task<string> GeneratePaymentNumber(CancellationToken cancellationToken = default)
    {
        var lastPayment = await _context.SupplierPayments
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNumber = 1;
        if (lastPayment != null && lastPayment.PaymentNumber.StartsWith("PAY-"))
        {
            var lastNumberStr = lastPayment.PaymentNumber.Substring(4);
            if (int.TryParse(lastNumberStr, out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"PAY-{nextNumber:D6}";
    }

    public async Task<IEnumerable<Purchase>> GetPendingPurchases(int supplierId, CancellationToken cancellationToken = default)
    {
        return await _context.Purchases
            .Where(p => p.SupplierId == supplierId && p.PaidAmount < p.InvoiceAmount)
            .Include(p => p.Supplier)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Sales>> GetPendingSales(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Where(s => s.CustomerId == customerId && s.PaidAmount < s.InvoiceAmount)
            .Include(s => s.Customer)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetSupplierDueBalance(int supplierId, CancellationToken cancellationToken = default)
    {
        var dueBalance = await _context.Purchases
            .Where(p => p.SupplierId == supplierId)
            .SumAsync(p => p.InvoiceAmount - p.PaidAmount, cancellationToken);

        var paidBalance = await _context.SupplierPayments
                    .Where(p => p.SupplierId == supplierId)
                    .SumAsync(p => p.PaymentAmount, cancellationToken);

        return dueBalance - paidBalance;
    }
}