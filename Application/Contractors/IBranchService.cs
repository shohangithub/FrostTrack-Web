using Application.Framework;

namespace Application.Contractors;

public interface IBranchService
{
    Task<IEnumerable<BranchListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<BranchListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<BranchResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<BranchResponse> AddAsync(BranchRequest request, CancellationToken cancellationToken = default);
    Task<BranchResponse> UpdateAsync(int id, BranchRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<Branch, bool>> predicate, CancellationToken cancellationToken = default);
    Task<string> GenerateCode(CancellationToken cancellationToken = default);
}
