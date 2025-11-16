

using Application.Framework;

namespace Application.Contractors;

public interface IUserService<T>
{
    Task<IEnumerable<UserListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<UserListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<UserResponse> GetByIdAsync(T id, CancellationToken cancellationToken = default);
    Task<UserResponse> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserResponse> AddAsync(UserRequest user, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateAsync(T id, UserRequest user, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(T id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(T id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<ApplicationUser, bool>> predicate, CancellationToken cancellationToken = default);
}
