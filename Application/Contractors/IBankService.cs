using Application.RequestDTO;
using Application.ReponseDTO;
using Application.Framework;

namespace Application.Contractors;

public interface IBankService
{
    Task<IEnumerable<BankListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<BankListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<BankResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<BankResponse> AddAsync(BankRequest bank, CancellationToken cancellationToken = default);
    Task<BankResponse> UpdateAsync(int id, BankRequest bank, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<Bank, bool>> predicate, CancellationToken cancellationToken = default);
    Task<string> GenerateCode(CancellationToken cancellationToken = default);
    Task<decimal> GetCurrentBalanceAsync(int bankId, CancellationToken cancellationToken = default);
}