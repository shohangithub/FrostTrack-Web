using Application.Framework;

namespace Application.Contractors;

public interface ISaleReturnService
{
    Task<IEnumerable<SaleReturnListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<SaleReturnListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<SaleReturnResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<SaleReturnResponse> AddAsync(SaleReturnRequest request, CancellationToken cancellationToken = default);
    Task<SaleReturnResponse> UpdateAsync(long id, SaleReturnRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<long> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<long>>> GetLookup(Expression<Func<SaleReturn, bool>> predicate, CancellationToken cancellationToken = default);
    Task<string> GenerateReturnNumber(CancellationToken cancellationToken = default);
}