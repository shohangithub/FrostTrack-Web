using Application.Contractors;
using Application.ReponseDTO;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SeasonArchiveController(ISeasonArchiveService seasonArchiveService) : ControllerBase
{
    /// <summary>
    /// Pre-archive preview: calculates completed bookings to archive, open bookings to carry forward,
    /// customer net dues to roll forward into opening balances, closing bank balances, and cash in hand.
    /// Purely read-only; makes zero database changes.
    /// </summary>
    [HttpGet("preview")]
    public async Task<ActionResult<SeasonArchivePreviewResponse>> Preview(
        [FromQuery] DateTime? cutoffDate,
        CancellationToken cancellationToken)
    {
        var date = cutoffDate ?? DateTime.UtcNow;
        var result = await seasonArchiveService.GetArchivePreviewAsync(date, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Executes the atomic season closing & batch archive operation.
    /// Archives completed bookings, past deliveries, past transactions.
    /// Rolls forward remaining stocks, customer net dues as opening balances, and bank/cash opening balances.
    /// Returns 409 Conflict if another archive operation is currently running.
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<SeasonArchiveResultResponse>> Execute(
        [FromBody] ExecuteSeasonArchiveRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await seasonArchiveService.ExecuteArchiveAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves historical season archive logs.
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<SeasonArchiveHistoryResponse>>> History(
        CancellationToken cancellationToken)
    {
        var result = await seasonArchiveService.GetArchiveHistoryAsync(cancellationToken);
        return Ok(result);
    }
}
