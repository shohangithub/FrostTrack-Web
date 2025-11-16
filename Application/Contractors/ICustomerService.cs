

using Application.Framework;

namespace Application.Contractors;

public interface ICustomerService
{
    Task<IEnumerable<CustomerListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<CustomerListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<CustomerResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomerResponse> AddAsync(CustomerRequest customer, CancellationToken cancellationToken = default);
    Task<CustomerResponse> UpdateAsync(int id, CustomerRequest customer, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<Customer, bool>> predicate, CancellationToken cancellationToken = default);
    Task<string> GenerateCode(CancellationToken cancellationToken = default);
}
