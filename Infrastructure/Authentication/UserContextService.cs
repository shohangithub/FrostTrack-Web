using Application.Contractors.Authentication;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Authentication;

public class UserContextService : IUserContextService
{
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IJwtUserService _jwtUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(
        ICurrentUserProvider currentUserProvider,
        IJwtUserService jwtUserService,
        IHttpContextAccessor httpContextAccessor)
    {
        _currentUserProvider = currentUserProvider;
        _jwtUserService = jwtUserService;
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUser GetCurrentUser()
    {
        // Try the enhanced CurrentUserProvider first
        var user = _currentUserProvider.GetCurrentUser();

        // If it returns anonymous user and we have a token, try JWT service
        if (user.Id == 0 && !string.IsNullOrEmpty(GetBearerToken()))
        {
            var jwtUser = GetCurrentUserFromJwtToken();
            if (jwtUser != null && jwtUser.Id > 0)
            {
                System.Diagnostics.Debug.WriteLine("✅ Fallback to JWT service successful!");
                return jwtUser;
            }
        }

        return user;
    }

    public CurrentUser? GetCurrentUserFromJwtToken(string? token = null)
    {
        try
        {
            token ??= GetBearerToken();

            if (string.IsNullOrEmpty(token))
            {
                System.Diagnostics.Debug.WriteLine("❌ No JWT token available");
                return null;
            }

            return _jwtUserService.GetUserFromToken(token);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error getting user from JWT: {ex.Message}");
            return null;
        }
    }

    public bool IsAuthenticated()
    {
        var user = GetCurrentUser();
        return user.Id > 0;
    }

    public string? GetUserId()
    {
        var user = GetCurrentUser();
        return user.Id > 0 ? user.Id.ToString() : null;
    }

    public string? GetUserEmail()
    {
        var user = GetCurrentUser();
        return user.Id > 0 ? user.Email : null;
    }

    public Guid? GetTenantId()
    {
        var user = GetCurrentUser();
        return user.TenantId != Guid.Empty ? user.TenantId : null;
    }

    public List<string>? GetUserRoles()
    {
        var user = GetCurrentUser();
        return user.Roles?.ToList();
    }

    private string? GetBearerToken()
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return null;
        }

        return authHeader.Substring("Bearer ".Length).Trim();
    }
}