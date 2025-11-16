using Application.Framework;

namespace Application.Contractors;

public interface ISalesService
{
    Task<IEnumerable<SalesListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<SalesListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<SalesResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<SalesResponse> AddAsync(SalesRequest request, CancellationToken cancellationToken = default);
    Task<SalesResponse> UpdateAsync(long id, SalesRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<long> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<long>>> GetLookup(Expression<Func<Sales, bool>> predicate, CancellationToken cancellationToken = default);
    Task<string> GenerateInvoiceNumber(CancellationToken cancellationToken = default);
}
