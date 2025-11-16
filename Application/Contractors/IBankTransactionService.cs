namespace Application.Contractors;

public interface IBankTransactionService
{
    Task<BankTransactionResponse> AddAsync(BankTransactionRequest bankTransaction, CancellationToken cancellationToken = default);
    Task<BankTransactionResponse> UpdateAsync(long id, BankTransactionRequest bankTransaction, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<long> ids, CancellationToken cancellationToken = default);
    Task<BankTransactionResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<BankTransactionListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<BankTransactionListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<long>>> GetLookup(Expression<Func<BankTransaction, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(long id, CancellationToken cancellationToken = default);
    Task<string> GenerateCode(CancellationToken cancellationToken = default);
}