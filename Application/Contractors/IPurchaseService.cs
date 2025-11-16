using Application.Framework;

namespace Application.Contractors;

public interface IPurchaseService
{
    Task<IEnumerable<PurchaseListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<PurchaseListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<PurchaseResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<PurchaseResponse> AddAsync(PurchaseRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseResponse> UpdateAsync(long id, PurchaseRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<long> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<long>>> GetLookup(Expression<Func<Purchase, bool>> predicate, CancellationToken cancellationToken = default);
    Task<string> GenerateInvoiceNumber(CancellationToken cancellationToken = default);
}
