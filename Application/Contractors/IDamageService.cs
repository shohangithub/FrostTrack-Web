using Application.RequestDTO;
using Application.ReponseDTO;
using Application.Framework;

namespace Application.Contractors;

public interface IDamageService
{
    Task<IEnumerable<DamageListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<DamageListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<DamageResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DamageResponse> AddAsync(DamageRequest damage, CancellationToken cancellationToken = default);
    Task<DamageResponse> UpdateAsync(int id, DamageRequest damage, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<Damage, bool>> predicate, CancellationToken cancellationToken = default);
    Task<string> GenerateCode(CancellationToken cancellationToken = default);
}