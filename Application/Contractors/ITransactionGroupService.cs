using Application.Framework;

namespace Application.Contractors;

public interface ITransactionGroupService
{
    Task<PaginationResult<TransactionGroupListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<TransactionGroupResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TransactionGroupResponse?> AddAsync(TransactionGroupRequest request, CancellationToken cancellationToken = default);
    Task<TransactionGroupResponse?> UpdateAsync(Guid id, TransactionGroupRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<Guid>>> GetLookup(CancellationToken cancellationToken = default);
}
