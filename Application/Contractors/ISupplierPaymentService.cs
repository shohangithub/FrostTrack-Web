using Application.Framework;

namespace Application.Contractors;

public interface ISupplierPaymentService
{
    Task<IEnumerable<SupplierPaymentListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<SupplierPaymentListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<SupplierPaymentResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<SupplierPaymentResponse> AddAsync(SupplierPaymentRequest request, CancellationToken cancellationToken = default);
    Task<SupplierPaymentResponse> UpdateAsync(long id, SupplierPaymentRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<long> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<long>>> GetLookup(Expression<Func<SupplierPayment, bool>> predicate, CancellationToken cancellationToken = default);
    Task<string> GeneratePaymentNumber(CancellationToken cancellationToken = default);
    Task<decimal> GetSupplierDueBalance(int supplierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PurchaseListResponse>> GetPendingPurchases(int supplierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SalesListResponse>> GetPendingSales(int customerId, CancellationToken cancellationToken = default);
}