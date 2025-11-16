using Application.Contractors.Authentication;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Infrastructure.Authentication;

public class CurrentUserProvider(IHttpContextAccessor _httpContextAccessor) : ICurrentUserProvider
{
    public CurrentUser GetCurrentUser()
    {
        // Check if HttpContext is available
        if (_httpContextAccessor.HttpContext == null)
        {
            System.Diagnostics.Debug.WriteLine("❌ HttpContext is null!");
            return GetAnonymousUser();
        }

        // Check if User is available
        if (_httpContextAccessor.HttpContext.User == null)
        {
            System.Diagnostics.Debug.WriteLine("❌ HttpContext.User is null!");
            return GetAnonymousUser();
        }

        // Try to get user info from JWT token directly if authentication failed
        if (_httpContextAccessor.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            System.Diagnostics.Debug.WriteLine($"❌ User is not authenticated. IsAuthenticated: {_httpContextAccessor.HttpContext.User.Identity?.IsAuthenticated}");

            // Try to extract user info directly from JWT token as fallback
            var userFromToken = TryGetUserFromJwtToken();
            if (userFromToken != null)
            {
                System.Diagnostics.Debug.WriteLine("✅ Extracted user info directly from JWT token!");
                return userFromToken;
            }

            return GetAnonymousUser();
        }

        var claims = _httpContextAccessor.HttpContext.User.Claims.ToList();

        // Debugging: Log all claims to see what's available
        System.Diagnostics.Debug.WriteLine($"✅ User is authenticated! Total claims found: {claims.Count}");
        foreach (var claim in claims)
        {
            System.Diagnostics.Debug.WriteLine($"  📋 Claim Type: {claim.Type}, Value: {claim.Value}");
        }

        return ExtractUserFromClaims(claims);
    }

    private CurrentUser ExtractUserFromClaims(List<Claim> claims)
    {
        var id = int.Parse(GetSingleClaimValue(CustomClaims.Id, claims) ?? GetSingleClaimValue(JwtRegisteredClaimNames.Sub, claims) ?? "0");
        var tenantId = Guid.Parse(GetSingleClaimValue(CustomClaims.Tenant, claims) ?? "00000000-0000-0000-0000-000000000000");
        var firstName = GetSingleClaimValue(CustomClaims.Id, claims) ?? GetSingleClaimValue(JwtRegisteredClaimNames.Name, claims) ?? GetSingleClaimValue(JwtRegisteredClaimNames.GivenName, claims) ?? "Unknown";
        var lastName = GetSingleClaimValue(JwtRegisteredClaimNames.FamilyName, claims) ?? "User";
        var email = GetSingleClaimValue(JwtRegisteredClaimNames.Email, claims) ?? "unknown@system.local";
        var branchId = int.Parse(GetSingleClaimValue(CustomClaims.BranchId, claims) ?? "0");
        var roles = GetClaimValues(CustomClaims.Role, claims) ?? new List<string>();
        var permissions = new List<string>(); // Add permissions logic if needed

        return new CurrentUser(id, tenantId, firstName, lastName, email, branchId, permissions, roles);
    }

    private CurrentUser? TryGetUserFromJwtToken()
    {
        try
        {
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                System.Diagnostics.Debug.WriteLine("❌ No Bearer token found in Authorization header");
                return null;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
            {
                System.Diagnostics.Debug.WriteLine("❌ Cannot read JWT token");
                return null;
            }

            var jsonToken = handler.ReadJwtToken(token);
            var claims = jsonToken.Claims.ToList();

            System.Diagnostics.Debug.WriteLine($"🔍 Extracted {claims.Count} claims directly from JWT token:");
            foreach (var claim in claims)
            {
                System.Diagnostics.Debug.WriteLine($"  📋 JWT Claim: {claim.Type} = {claim.Value}");
            }

            return ExtractUserFromClaims(claims);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error reading JWT token: {ex.Message}");
            return null;
        }
    }

    private CurrentUser GetAnonymousUser()
    {
        return new CurrentUser(0, Guid.Empty, "Anonymous", "User", "anonymous@system.local", 0, new List<string>(), new List<string>());
    }

    private List<string>? GetClaimValues(string claimType, List<Claim>? claims = null)
    {
        claims ??= _httpContextAccessor.HttpContext?.User?.Claims?.ToList();
        return claims?.Where(claim => claim.Type == claimType)
                    .Select(claim => claim.Value)
                    .ToList();
    }

    private string? GetSingleClaimValue(string claimType, List<Claim>? claims = null)
    {
        claims ??= _httpContextAccessor.HttpContext?.User?.Claims?.ToList();
        return claims?.FirstOrDefault(claim => claim.Type == claimType)?.Value;
    }
}
