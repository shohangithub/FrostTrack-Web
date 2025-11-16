using Application.Contractors;
using Application.ReponseDTO;
using Application.RequestDTO;
using Application.Framework;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DamageController : ControllerBase
{
    private readonly IDamageService _damageService;

    public DamageController(IDamageService damageService)
    {
        _damageService = damageService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DamageListResponse>>> GetDamages(CancellationToken cancellationToken)
    {
        var response = await _damageService.ListAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("get-with-pagination")]
    public async Task<ActionResult<PaginationResult<DamageListResponse>>> GetDamagesWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        var response = await _damageService.PaginationListAsync(requestQuery, cancellationToken);
        return response;
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<IEnumerable<Lookup<int>>>> GetDamageLookup(CancellationToken cancellationToken)
    {
        var response = await _damageService.GetLookup(x => x.IsActive, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DamageResponse>> GetDamage(int id, CancellationToken cancellationToken)
    {
        var response = await _damageService.GetByIdAsync(id, cancellationToken);
        return response;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DamageResponse>> PutDamage(int id, DamageRequest damage)
    {
        var response = await _damageService.UpdateAsync(id, damage);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<DamageResponse>> PostDamage(DamageRequest damage, CancellationToken cancellationToken)
    {
        return await _damageService.AddAsync(damage, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteDamage(int id, CancellationToken cancellationToken)
    {
        return await _damageService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<int> ids, CancellationToken cancellationToken)
    {
        return await _damageService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsDamageExists")]
    public async ValueTask<bool> IsDamageExists([FromQuery] int id, CancellationToken cancellationToken)
    {
        var response = await _damageService.IsExistsAsync(id, cancellationToken);
        return response;
    }

    [HttpGet("generate-code")]
    public async ValueTask<CodeResponse> GenerateCode(CancellationToken cancellationToken)
    {
        var response = await _damageService.GenerateCode(cancellationToken);
        return new CodeResponse(response);
    }
}