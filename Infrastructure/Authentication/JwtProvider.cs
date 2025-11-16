
using Application.Contractors.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Authentication.TokenGenerator;

public sealed class JwtProvider : IJwtProvider
{
    private readonly JwtOptions _options;
    public JwtProvider(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }
    public string GenerateAccessToken(TokenUser user)
    {

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);


        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.email),
            new(JwtRegisteredClaimNames.Name, user.firstName),
            new(JwtRegisteredClaimNames.FamilyName, user.lastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            new(CustomClaims.Id, user.id.ToString()),
            new(CustomClaims.Tenant, user.tenantId.ToString()),
            new(CustomClaims.BranchId, user.branchId?.ToString() ?? "0"),
        };

        // Add individual role claims for ASP.NET Core authorization
        foreach (var role in user.roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }


        // user.permissions?.ForEach(permission => claims.Add(new(CustomClaims.Permissions, permission)));



        // Create the JWT token
        var token = new JwtSecurityToken(
            claims: claims,
            issuer: _options.Issuer,
            audience: _options.Audience,
            signingCredentials: signingCredentials,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_options.TokenExpirationInMinutes))
        );

        var serializedToken = new JwtSecurityTokenHandler().WriteToken(token);
        return serializedToken;
    }
}
