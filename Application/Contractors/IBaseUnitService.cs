

using Application.Framework;

namespace Application.Contractors;

public interface IBaseUnitService
{
    Task<IEnumerable<BaseUnitListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<BaseUnitListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<BaseUnitResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<BaseUnitResponse> AddAsync(BaseUnitRequest user, CancellationToken cancellationToken = default);
    Task<BaseUnitResponse> UpdateAsync(int id, BaseUnitRequest user, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<BaseUnit, bool>> predicate, CancellationToken cancellationToken = default);
}
