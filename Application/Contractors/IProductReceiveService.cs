using Application.Framework;

namespace Application.Contractors;

public interface IProductReceiveService
{
    Task<IEnumerable<ProductReceiveListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<ProductReceiveListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<ProductReceiveResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ProductReceiveResponse> AddAsync(ProductReceiveRequest request, CancellationToken cancellationToken = default);
    Task<ProductReceiveResponse> UpdateAsync(long id, ProductReceiveRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<long> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<long>>> GetLookup(Expression<Func<ProductReceive, bool>> predicate, CancellationToken cancellationToken = default);
    Task<string> GenerateReceiveNumber(CancellationToken cancellationToken = default);
}
