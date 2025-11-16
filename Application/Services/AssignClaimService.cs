namespace Application.Services;

public class AssignClaimService : IAssignClaimService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AssignClaimService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> AddClaimAsync(int userId, string key, string value, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;
        var res = await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim(key, value));
        return res.Succeeded;
    }

    public async Task<bool> RemoveClaimAsync(int userId, string key, string value, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;
        var res = await _userManager.RemoveClaimAsync(user, new System.Security.Claims.Claim(key, value));
        return res.Succeeded;
    }

    public async Task<IEnumerable<Application.ReponseDTO.UserClaimsResponse>> GetAllUserClaimsAsync(CancellationToken cancellationToken = default)
    {
        var users = _userManager.Users.Select(u => new { u.Id, u.UserName, u.Email }).ToList();
        var result = new List<Application.ReponseDTO.UserClaimsResponse>();
        foreach (var u in users)
        {
            var appUser = await _userManager.FindByIdAsync(u.Id.ToString());
            if (appUser == null) continue;
            var claims = await _userManager.GetClaimsAsync(appUser);
            var claimDtos = claims.Select(c => new ClaimResponse(c.Type ?? string.Empty, c.Value ?? string.Empty));
            result.Add(new UserClaimsResponse(appUser.Id, appUser.UserName ?? string.Empty, appUser.Email ?? string.Empty, claimDtos));
        }
        return result;
    }
}
