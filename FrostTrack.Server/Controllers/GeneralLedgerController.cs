using Application.Contractors;
using Application.ReponseDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class GeneralLedgerController : ControllerBase
{
    private readonly IGeneralLedgerService _generalLedgerService;

    public GeneralLedgerController(IGeneralLedgerService generalLedgerService)
    {
        _generalLedgerService = generalLedgerService;
    }

    [HttpGet]
    public async Task<ActionResult<GeneralLedgerResponse>> GetGeneralLedger([FromQuery] DateTime reportDate, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _generalLedgerService.GetGeneralLedgerAsync(reportDate, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while generating general ledger report.", error = ex.Message });
        }
    }

    [HttpGet("range")]
    public async Task<ActionResult<GeneralLedgerRangeResponse>> GetGeneralLedgerRange(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _generalLedgerService.GetGeneralLedgerRangeAsync(fromDate, toDate, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while generating general ledger range report.", error = ex.Message });
        }
    }
}
