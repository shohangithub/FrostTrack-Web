

using Application.Framework;

namespace Application.Contractors;

public interface IUnitConversionService
{
    Task<IEnumerable<UnitConversionListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<UnitConversionListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<UnitConversionResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UnitConversionResponse> AddAsync(UnitConversionRequest user, CancellationToken cancellationToken = default);
    Task<UnitConversionResponse> UpdateAsync(int id, UnitConversionRequest user, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<UnitConversion, bool>> predicate, CancellationToken cancellationToken = default);
}
