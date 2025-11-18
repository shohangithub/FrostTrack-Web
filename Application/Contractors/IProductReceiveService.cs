using Application.Framework;

namespace Application.Contractors;

public interface IProductReceiveService
{
    Task<IEnumerable<ProductReceiveListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<ProductReceiveListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<ProductReceiveResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductReceiveResponse> AddAsync(ProductReceiveRequest request, CancellationToken cancellationToken = default);
    Task<ProductReceiveResponse> UpdateAsync(Guid id, ProductReceiveRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<Guid>>> GetLookup(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default);
    Task<string> GenerateReceiveNumber(CancellationToken cancellationToken = default);
}
