using Application.Framework;

namespace Application.Contractors;

public interface IAssignClaimService
{
    Task<bool> AddClaimAsync(int userId, string key, string value, CancellationToken cancellationToken = default);
    Task<bool> RemoveClaimAsync(int userId, string key, string value, CancellationToken cancellationToken = default);
    Task<IEnumerable<global::Application.ReponseDTO.UserClaimsResponse>> GetAllUserClaimsAsync(CancellationToken cancellationToken = default);
}
