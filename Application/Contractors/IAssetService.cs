using Application.RequestDTO;
using Application.ReponseDTO;
using Application.Framework;

namespace Application.Contractors;

public interface IAssetService
{
    Task<IEnumerable<AssetListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<AssetListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<AssetResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AssetResponse> AddAsync(AssetRequest asset, CancellationToken cancellationToken = default);
    Task<AssetResponse> UpdateAsync(int id, AssetRequest asset, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<Asset, bool>> predicate, CancellationToken cancellationToken = default);
    Task<string> GenerateCode(CancellationToken cancellationToken = default);
    Task<decimal> GetCurrentValueAsync(int assetId, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetDistinctAssetTypesAsync(CancellationToken cancellationToken = default);
}