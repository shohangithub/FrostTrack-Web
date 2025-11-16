using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
namespace Infrastructure.Authentication;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var role = context.User.Claims.FirstOrDefault(x => x.Type == CustomClaims.Role)?.Value;

        if (role == requirement.Permission)
        {
            context.Succeed(requirement);
        }
        else if (role == nameof(Roles.Admin) && (
               requirement.Permission == nameof(ERoles.Admin)
            || requirement.Permission == nameof(ERoles.Standard))
            )
        {
            context.Succeed(requirement);
        }
        else if (role == nameof(ERoles.MasterAdmin) && (
               requirement.Permission == nameof(ERoles.MasterAdmin)
            || requirement.Permission == nameof(ERoles.Admin)
            || requirement.Permission == nameof(ERoles.Standard))
            )
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }


        return Task.CompletedTask;
    }
}
