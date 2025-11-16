using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Application.RequestDTO;
using Application.ReponseDTO;
using Application.Contractors;
using Domain;

namespace FrostTrack.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssignClaimController : ControllerBase
{
    private readonly IAssignClaimService _service;

    public AssignClaimController(IAssignClaimService service)
    {
        _service = service;
    }

    [HttpPost("users/{id:int}/claims")]
    public async Task<IActionResult> AddClaim(int id, [FromBody] AssignClaimRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Key)) return BadRequest("Key and Value required");
        var res = await _service.AddClaimAsync(id, request.Key, request.Value);
        return res ? Ok(new AssignClaimResponse(id, request.Key, request.Value)) : BadRequest("Failed to add claim");
    }

    [HttpDelete("users/{id:int}/claims")]
    public async Task<IActionResult> RemoveClaim(int id, [FromBody] AssignClaimRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Key)) return BadRequest("Key and Value required");
        var res = await _service.RemoveClaimAsync(id, request.Key, request.Value);
        return res ? Ok() : BadRequest("Failed to remove claim");
    }

    // GET: api/assignclaim/users/claims
    [HttpGet("users/claims")]
    public async Task<IActionResult> GetAllUserClaims()
    {
        var res = await _service.GetAllUserClaimsAsync();
        return Ok(res);
    }
}
