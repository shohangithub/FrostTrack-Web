

using Application.Framework;

namespace Application.Contractors;

public interface IProductCategoryService
{
    Task<IEnumerable<ProductCategoryListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<ProductCategoryListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<ProductCategoryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductCategoryResponse> AddAsync(ProductCategoryRequest user, CancellationToken cancellationToken = default);
    Task<ProductCategoryResponse> UpdateAsync(int id, ProductCategoryRequest user, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<ProductCategory, bool>> predicate, CancellationToken cancellationToken = default);
}
