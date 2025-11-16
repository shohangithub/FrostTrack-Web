using Application.Framework;

namespace Application.Contractors;

public interface IProductService
{
    Task<IEnumerable<ProductListResponse>> ListAsync(Expression<Func<Product, bool>> predicate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductLisWithStockResponse>> ListwithStockAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<ProductListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductResponse> AddAsync(ProductRequest user, CancellationToken cancellationToken = default);
    Task<ProductResponse> UpdateAsync(int id, ProductRequest user, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<Product, bool>> predicate, CancellationToken cancellationToken = default);
    Task<string> GenerateCode(CancellationToken cancellationToken = default);
}
