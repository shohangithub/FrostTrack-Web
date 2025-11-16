namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LoginController : ControllerBase
{
    private readonly IUserTokenService _tokenService;
    public LoginController(IUserTokenService tokenService)
    {
        _tokenService = tokenService;
    }
    [ApiKey]
    [HttpPost]
    public async Task<TokenResponse> Post([FromBody] LoginRequest loginRequest, CancellationToken cancellationToken)
    {
        return await _tokenService.GetUserToken(loginRequest.Email, loginRequest.Password, cancellationToken);
    }
}
