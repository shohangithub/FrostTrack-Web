using Application.Framework;

namespace Application.Contractors;

public interface ITransactionHeadService
{
    Task<IEnumerable<TransactionHeadListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<TransactionHeadListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<TransactionHeadResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TransactionHeadResponse> AddAsync(TransactionHeadRequest request, CancellationToken cancellationToken = default);
    Task<TransactionHeadResponse> UpdateAsync(Guid id, TransactionHeadRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<Guid>>> GetLookup(Expression<Func<TransactionHead, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IEnumerable<TransactionHeadLookup>> GetTransactionLookup(CancellationToken cancellationToken = default);
}
