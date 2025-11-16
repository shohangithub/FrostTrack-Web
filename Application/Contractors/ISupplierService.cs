

using Application.Framework;

namespace Application.Contractors;

public interface ISupplierService
{
    Task<IEnumerable<SupplierListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<SupplierListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<SupplierResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SupplierResponse> AddAsync(SupplierRequest user, CancellationToken cancellationToken = default);
    Task<SupplierResponse> UpdateAsync(int id, SupplierRequest user, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<Supplier, bool>> predicate, CancellationToken cancellationToken = default);
    Task<string> GenerateCode(CancellationToken cancellationToken = default);
}
