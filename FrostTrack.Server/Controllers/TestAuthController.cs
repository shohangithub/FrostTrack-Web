using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Contractors.Authentication;
using Infrastructure.Authentication;
using System.IdentityModel.Tokens.Jwt;

namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestAuthController : ControllerBase
{
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IJwtUserService _jwtUserService;
    private readonly IUserContextService _userContextService;

    public TestAuthController(ICurrentUserProvider currentUserProvider, IJwtUserService jwtUserService, IUserContextService userContextService)
    {
        _currentUserProvider = currentUserProvider;
        _jwtUserService = jwtUserService;
        _userContextService = userContextService;
    }

    [HttpGet("anonymous")]
    public IActionResult TestAnonymous()
    {
        var user = _currentUserProvider.GetCurrentUser();

        // Get Authorization header
        var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

        return Ok(new
        {
            IsAuthenticated = HttpContext.User.Identity?.IsAuthenticated,
            UserName = HttpContext.User.Identity?.Name,
            ClaimsCount = HttpContext.User.Claims.Count(),
            CurrentUser = user,
            AuthorizationHeader = authHeader?.Substring(0, Math.Min(50, authHeader?.Length ?? 0)) + "...",
            Claims = HttpContext.User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }

    [HttpGet("authenticated")]
    [Authorize]
    public IActionResult TestAuthenticated()
    {
        var user = _currentUserProvider.GetCurrentUser();

        // Get Authorization header
        var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

        return Ok(new
        {
            IsAuthenticated = HttpContext.User.Identity?.IsAuthenticated,
            UserName = HttpContext.User.Identity?.Name,
            ClaimsCount = HttpContext.User.Claims.Count(),
            CurrentUser = user,
            AuthorizationHeader = authHeader?.Substring(0, Math.Min(50, authHeader?.Length ?? 0)) + "...",
            Claims = HttpContext.User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }

    [HttpGet("test-current-user")]
    [AllowAnonymous] // Allow anonymous to test both scenarios
    public IActionResult TestCurrentUser()
    {
        System.Diagnostics.Debug.WriteLine("🔍 Testing CurrentUserProvider...");

        var currentUser = _currentUserProvider.GetCurrentUser();
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();

        var result = new
        {
            Message = "Testing CurrentUserProvider",
            HasAuthHeader = !string.IsNullOrEmpty(authHeader),
            AuthHeader = authHeader,
            HttpContextAvailable = HttpContext != null,
            UserAvailable = HttpContext?.User != null,
            IsAuthenticated = HttpContext?.User?.Identity?.IsAuthenticated,
            ClaimsCount = HttpContext?.User?.Claims?.Count() ?? 0,
            CurrentUser = currentUser,
            Timestamp = DateTime.UtcNow
        };

        return Ok(result);
    }

    [HttpGet("test-jwt-service")]
    [AllowAnonymous]
    public IActionResult TestJwtService()
    {
        System.Diagnostics.Debug.WriteLine("🔍 Testing JwtUserService...");

        var authHeader = Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return BadRequest(new { Message = "No Bearer token provided" });
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        var isValid = _jwtUserService.ValidateToken(token);
        var claims = _jwtUserService.GetClaimsFromToken(token);
        var user = _jwtUserService.GetUserFromToken(token);

        return Ok(new
        {
            Message = "Testing JwtUserService",
            TokenValid = isValid,
            ClaimsCount = claims?.Count ?? 0,
            Claims = claims?.Select(c => new { c.Type, c.Value }).ToArray(),
            ExtractedUser = user,
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("debug-jwt")]
    public IActionResult DebugJWT()
    {
        var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Ok(new { Error = "No Bearer token found", AuthHeader = authHeader });
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // Test both approaches
            var currentUserResult = _currentUserProvider.GetCurrentUser();
            var jwtServiceResult = _jwtUserService.GetUserFromToken(token);

            return Ok(new
            {
                TokenValid = true,
                Issuer = jsonToken.Issuer,
                Audience = jsonToken.Audiences.FirstOrDefault(),
                ExpiresAt = jsonToken.ValidTo,
                IssuedAt = jsonToken.ValidFrom,
                Claims = jsonToken.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                HttpContextAuth = new
                {
                    IsAuthenticated = HttpContext.User.Identity?.IsAuthenticated,
                    AuthenticationType = HttpContext.User.Identity?.AuthenticationType,
                    Name = HttpContext.User.Identity?.Name,
                    Claims = HttpContext.User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
                },
                CurrentUserProvider = currentUserResult,
                JwtUserService = jwtServiceResult
            });
        }
        catch (Exception ex)
        {
            return Ok(new { Error = $"Invalid JWT token: {ex.Message}", Token = token.Substring(0, Math.Min(50, token.Length)) + "..." });
        }
    }
}