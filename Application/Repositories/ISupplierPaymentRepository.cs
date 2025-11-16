using Application.Framework;

namespace Application.Repositories;

public interface ISupplierPaymentRepository
{
    IQueryable<SupplierPayment> Query();
    Task<SupplierPayment?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<SupplierPaymentResponse> ManageUpdate(SupplierPaymentRequest request, SupplierPayment existingData, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<string> GeneratePaymentNumber(CancellationToken cancellationToken = default);
    Task<IEnumerable<Purchase>> GetPendingPurchases(int supplierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Sales>> GetPendingSales(int customerId, CancellationToken cancellationToken = default);
    Task<decimal> GetSupplierDueBalance(int supplierId, CancellationToken cancellationToken = default);
}