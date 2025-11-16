namespace Application.Contractors.Authentication;

/// <summary>
/// Enhanced service for getting current user information with multiple fallback methods
/// </summary>
public interface IUserContextService
{
    CurrentUser GetCurrentUser();
    CurrentUser? GetCurrentUserFromJwtToken(string? token = null);
    bool IsAuthenticated();
    string? GetUserId();
    string? GetUserEmail();
    Guid? GetTenantId();
    List<string>? GetUserRoles();
}