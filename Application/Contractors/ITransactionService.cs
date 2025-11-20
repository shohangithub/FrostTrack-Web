namespace Application.Contractors;

public interface ITransactionService
{
    Task<TransactionResponse> AddAsync(TransactionRequest request, CancellationToken cancellationToken = default);
    Task<TransactionResponse> UpdateAsync(Guid id, TransactionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default);
    Task<TransactionResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TransactionListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<TransactionListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<string> GenerateTransactionCode(CancellationToken cancellationToken = default);
    Task<TransactionSummaryResponse> GetSummaryAsync(DateTime startDate, DateTime endDate, int? branchId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<CashFlowResponse>> GetCashFlowAsync(DateTime startDate, DateTime endDate, int? branchId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<Guid>>> GetLookup(Expression<Func<Transaction, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> UnarchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
