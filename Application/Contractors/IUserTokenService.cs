namespace Application.Contractors;

public interface IUserTokenService
{
    Task<UserResponseForToken> GetByEmailAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<TokenResponse> GetUserToken(string email, string password, CancellationToken cancellationToken = default);
}