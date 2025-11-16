namespace Application.Services;

public class UserTokenService : IUserTokenService
{

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtProvider _jwtProvider;

    public UserTokenService(IJwtProvider jwtProvider, UserManager<ApplicationUser> userManager)
    {
        _jwtProvider = jwtProvider;
        _userManager = userManager;
    }





    public async Task<UserResponseForToken> GetByEmailAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        // Take user from UserManager
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
        {
            return null;
        }

        // get user roles
        var roles = await _userManager.GetRolesAsync(user);
        // create response
        var response = new UserResponseForToken(user.Id, user.TenantId, user.UserName, user.Email, roles, user.BranchId, user.IsActive, user.Status);

        return response;
    }

    public async Task<TokenResponse> GetUserToken(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await GetByEmailAsync(email, password, cancellationToken);
        if (user is null) throw new Exception("User not found !");

        var token = _jwtProvider.GenerateAccessToken(new TokenUser(
            id: user.Id,
            tenantId: user.TenantId,
            email: user.Email,
            firstName: user.UserName,
            lastName: user.UserName,
            branchId: user.BranchId,
            roles: user.RoleNames,
            permissions: null
            ));
        return new TokenResponse(token, user.Email, user.UserName, user.UserName);
    }

}
