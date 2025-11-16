using Application.Contractors.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Authentication;

public interface IJwtUserService
{
    CurrentUser? GetUserFromToken(string token);
    bool ValidateToken(string token);
    List<Claim>? GetClaimsFromToken(string token);
}

public class JwtUserService : IJwtUserService
{
    private readonly IConfiguration _configuration;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public JwtUserService(IConfiguration configuration)
    {
        _configuration = configuration;

        var secretKey = _configuration["JWT:SecretKey"] ?? throw new InvalidOperationException("JWT:SecretKey is not configured");
        var issuer = _configuration["JWT:Issuer"] ?? throw new InvalidOperationException("JWT:Issuer is not configured");
        var audience = _configuration["JWT:Audience"] ?? throw new InvalidOperationException("JWT:Audience is not configured");

        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    public CurrentUser? GetUserFromToken(string token)
    {
        try
        {
            var claims = GetClaimsFromToken(token);
            if (claims == null || !claims.Any())
            {
                System.Diagnostics.Debug.WriteLine("❌ No claims found in token");
                return null;
            }

            var id = int.Parse(GetClaimValue(claims, CustomClaims.Id) ?? GetClaimValue(claims, JwtRegisteredClaimNames.Sub) ?? "0");
            var tenantId = Guid.Parse(GetClaimValue(claims, CustomClaims.Tenant) ?? "00000000-0000-0000-0000-000000000000");
            var firstName = GetClaimValue(claims, JwtRegisteredClaimNames.Name) ?? GetClaimValue(claims, JwtRegisteredClaimNames.GivenName) ?? "Unknown";
            var lastName = GetClaimValue(claims, JwtRegisteredClaimNames.FamilyName) ?? "User";
            var email = GetClaimValue(claims, JwtRegisteredClaimNames.Email) ?? "unknown@system.local";
            var branchId = int.Parse(GetClaimValue(claims, CustomClaims.BranchId) ?? "0");
            var roles = GetClaimValues(claims, ClaimTypes.Role) ?? new List<string>();
            var permissions = new List<string>(); // Add permissions logic if needed

            System.Diagnostics.Debug.WriteLine($"✅ Successfully extracted user from JWT: ID={id}, Email={email}, TenantId={tenantId}");

            return new CurrentUser(id, tenantId, firstName, lastName, email, branchId, permissions, roles);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error extracting user from token: {ex.Message}");
            return null;
        }
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);

            System.Diagnostics.Debug.WriteLine("✅ Token validation successful");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Token validation failed: {ex.Message}");
            return false;
        }
    }

    public List<Claim>? GetClaimsFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            if (!tokenHandler.CanReadToken(token))
            {
                System.Diagnostics.Debug.WriteLine("❌ Cannot read JWT token");
                return null;
            }

            var jsonToken = tokenHandler.ReadJwtToken(token);
            var claims = jsonToken.Claims.ToList();

            System.Diagnostics.Debug.WriteLine($"📋 Extracted {claims.Count} claims from JWT token");
            foreach (var claim in claims)
            {
                System.Diagnostics.Debug.WriteLine($"  Claim: {claim.Type} = {claim.Value}");
            }

            return claims;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error reading JWT token: {ex.Message}");
            return null;
        }
    }

    private string? GetClaimValue(List<Claim> claims, string claimType)
    {
        return claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }

    private List<string> GetClaimValues(List<Claim> claims, string claimType)
    {
        return claims.Where(c => c.Type == claimType).Select(c => c.Value).ToList();
    }
}