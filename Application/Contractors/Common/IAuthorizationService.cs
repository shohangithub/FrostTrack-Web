using Application.Common;
using ErrorOr;

namespace Application.Contractors.Common
{
    public interface IAuthorizationService
    {
        ErrorOr<Success> AuthorizeCurrentUser(
            IAuthorizeableRequest request,
            List<string> requiredRoles,
            List<string> requiredPermissions,
            List<string> requiredPolicies);
    }
}
