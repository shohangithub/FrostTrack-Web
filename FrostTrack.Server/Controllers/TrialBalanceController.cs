using Application.Contractors;
using Application.ReponseDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class TrialBalanceController : ControllerBase
{
    private readonly ITrialBalanceService _service;

    public TrialBalanceController(ITrialBalanceService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get trial balance report for a specific date
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<TrialBalanceSummaryResponse>> GetTrialBalance(
        [FromQuery] DateTime reportDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetTrialBalanceAsync(reportDate, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while generating trial balance report.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get trial balance report for a date range
    /// </summary>
    [HttpGet("range")]
    public async Task<ActionResult<TrialBalanceSummaryResponse>> GetTrialBalanceRange(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetTrialBalanceRangeAsync(fromDate, toDate, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while generating trial balance range report.", error = ex.Message });
        }
    }
}
