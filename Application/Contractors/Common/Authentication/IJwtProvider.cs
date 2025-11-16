namespace Application.Contractors.Authentication;

public interface IJwtProvider
{
    string GenerateRefreshToken();
    string GenerateAccessToken(TokenUser user);
}
