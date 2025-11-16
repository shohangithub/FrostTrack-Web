using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Domain;

namespace FrostTrack.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SecurityController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public SecurityController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // Roles
    [HttpGet("roles")]
    public IActionResult GetRoles() => Ok(_roleManager.Roles.Select(r => new { r.Id, r.Name }));

    [HttpPost("roles")]
    public async Task<ActionResult<RoleResponse>> CreateRole([FromBody] RoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("roleName required");
        if (await _roleManager.RoleExistsAsync(request.Name)) return Conflict("Role already exists");
        var r = new ApplicationRole { Name = request.Name };
        var res = await _roleManager.CreateAsync(r);
        return res.Succeeded ? Ok(r) : BadRequest(res.Errors);
    }


    [HttpDelete("roles/{roleName}")]
    public async Task<IActionResult> DeleteRole(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return NotFound();
        var res = await _roleManager.DeleteAsync(role);
        return res.Succeeded ? NoContent() : BadRequest(res.Errors);
    }

    [HttpPut("roles/{id:int}")]
    public async Task<ActionResult<RoleResponse>> UpdateRole(int id, [FromBody] RoleRequest request)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) return NotFound();
        if (request == null || string.IsNullOrWhiteSpace(request.Name)) return BadRequest("roleName required");
        role.Name = request.Name;
        var res = await _roleManager.UpdateAsync(role);
        return res.Succeeded ? Ok(new RoleResponse(role.Id, role.Name)) : BadRequest(res.Errors);
    }

    [HttpPost("roles/batch-delete")]
    public async Task<IActionResult> BatchDeleteRoles([FromBody] string[] roleNames)
    {
        if (roleNames == null || roleNames.Length == 0) return BadRequest("role names required");
        foreach (var rn in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(rn);
            if (role != null)
            {
                await _roleManager.DeleteAsync(role);
            }
        }
        return Ok();
    }

    // Users
    [HttpGet("users")]
    public IActionResult GetUsers() => Ok(_userManager.Users.Select(u => new { u.Id, u.UserName, u.Email, u.IsActive }));

    [HttpGet("users/{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var u = await _userManager.FindByIdAsync(id.ToString());
        if (u == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(u);
        var claims = await _userManager.GetClaimsAsync(u);
        return Ok(new { u.Id, u.UserName, u.Email, u.IsActive, Roles = roles, Claims = claims.Select(c => new { c.Type, c.Value }) });
    }

    public record CreateUserDto(string UserName, string Email, string Password, string Role);

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        var user = new ApplicationUser { UserName = dto.UserName, Email = dto.Email, IsActive = true };
        var res = await _userManager.CreateAsync(user, dto.Password);
        if (!res.Succeeded) return BadRequest(res.Errors);
        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            if (!await _roleManager.RoleExistsAsync(dto.Role))
                await _roleManager.CreateAsync(new ApplicationRole { Name = dto.Role });
            await _userManager.AddToRoleAsync(user, dto.Role);
        }
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { user.Id, user.UserName, user.Email });
    }

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var u = await _userManager.FindByIdAsync(id.ToString());
        if (u == null) return NotFound();
        var res = await _userManager.DeleteAsync(u);
        return res.Succeeded ? NoContent() : BadRequest(res.Errors);
    }

    [HttpPost("users/{id:int}/claims")]
    public async Task<IActionResult> AddClaim(int id, [FromBody] KeyValuePair<string, string> claim)
    {
        var u = await _userManager.FindByIdAsync(id.ToString());
        if (u == null) return NotFound();
        var res = await _userManager.AddClaimAsync(u, new System.Security.Claims.Claim(claim.Key, claim.Value));
        return res.Succeeded ? Ok() : BadRequest(res.Errors);
    }

    [HttpDelete("users/{id:int}/claims")]
    public async Task<IActionResult> RemoveClaim(int id, [FromBody] KeyValuePair<string, string> claim)
    {
        var u = await _userManager.FindByIdAsync(id.ToString());
        if (u == null) return NotFound();
        var res = await _userManager.RemoveClaimAsync(u, new System.Security.Claims.Claim(claim.Key, claim.Value));
        return res.Succeeded ? Ok() : BadRequest(res.Errors);
    }

    // Assign role to existing user
    [HttpPost("users/{id:int}/roles")]
    public async Task<IActionResult> AddRoleToUser(int id, [FromBody] string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return BadRequest("roleName required");
        var u = await _userManager.FindByIdAsync(id.ToString());
        if (u == null) return NotFound();
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var createRes = await _roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            if (!createRes.Succeeded) return BadRequest(createRes.Errors);
        }
        var res = await _userManager.AddToRoleAsync(u, roleName);
        return res.Succeeded ? Ok() : BadRequest(res.Errors);
    }

    // Remove role from existing user
    [HttpDelete("users/{id:int}/roles")]
    public async Task<IActionResult> RemoveRoleFromUser(int id, [FromQuery] string role)
    {
        if (string.IsNullOrWhiteSpace(role)) return BadRequest("role required");
        var u = await _userManager.FindByIdAsync(id.ToString());
        if (u == null) return NotFound();
        var res = await _userManager.RemoveFromRoleAsync(u, role);
        return res.Succeeded ? Ok() : BadRequest(res.Errors);
    }
}
