namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RecurringChargeManagementController(IRecurringChargeManagementService recurringChargeManagementService) : ControllerBase
{
    /// <summary>
    /// Read-only preview: shows which bookings would be updated and the total recurring-charge
    /// amount if Apply is executed now (or for a given date). Makes no DB changes.
    /// </summary>
    [HttpGet("preview")]
    public async Task<ActionResult<RecurringChargePreviewResponse>> Preview(
        [FromQuery] DateTime? asOfDate,
        CancellationToken cancellationToken)
    {
        var date = (asOfDate?.ToUniversalTime()) ?? DateTime.UtcNow;
        var result = await recurringChargeManagementService.PreviewAsync(date, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Applies recurring charges, advances LastRecurringChargeDate on affected BookingDetails, and
    /// writes an immutable RecurringChargeRun audit record.
    /// Returns 409 Conflict if another run is already in progress for this tenant.
    /// </summary>
    [HttpPost("apply")]
    public async Task<ActionResult<RecurringChargeRunResponse>> Apply(
        [FromBody] RecurringChargeRunRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await recurringChargeManagementService.ApplyManualRecurringChargeAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Returns the most recent recurring-charge run records (newest first).</summary>
    [HttpGet("history")]
    public async Task<ActionResult<List<RecurringChargeRunResponse>>> History(
        [FromQuery] int take = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await recurringChargeManagementService.GetHistoryAsync(take, cancellationToken);
        return Ok(result);
    }
}
