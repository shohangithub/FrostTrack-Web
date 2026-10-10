using Application.Contractors;
using Application.ReponseDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class BalanceSheetController : ControllerBase
{
    private readonly IBalanceSheetService _service;

    public BalanceSheetController(IBalanceSheetService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get balance sheet report for a specific date
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<BalanceSheetSummaryResponse>> GetBalanceSheet(
        [FromQuery] DateTime reportDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetBalanceSheetAsync(reportDate, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while generating balance sheet report.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get comparative balance sheet between two as-of dates
    /// </summary>
    [HttpGet("comparative")]
    public async Task<ActionResult<BalanceSheetComparativeResponse>> GetComparativeBalanceSheet(
        [FromQuery] DateTime date1,
        [FromQuery] DateTime date2,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetComparativeBalanceSheetAsync(date1, date2, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while generating comparative balance sheet report.", error = ex.Message });
        }
    }
}
