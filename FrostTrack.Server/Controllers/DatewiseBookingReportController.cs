using Application.Contractors;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DatewiseBookingReportController : ControllerBase
{
    private readonly IDatewiseBookingReportService _datewiseBookingReportService;

    public DatewiseBookingReportController(IDatewiseBookingReportService datewiseBookingReportService)
    {
        _datewiseBookingReportService = datewiseBookingReportService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDatewiseBookingReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? customerId,
        [FromQuery] int? productId,
        CancellationToken cancellationToken)
    {
        // Default to today if no dates provided
        if (fromDate == null || toDate == null)
        {
            var today = DateTime.UtcNow.Date;
            fromDate ??= today;
            toDate ??= today;
        }

        var startDate = fromDate.Value.Date.ToUniversalTime();
        var endDate = toDate.Value.Date.ToUniversalTime();
        var result = await _datewiseBookingReportService.GetDatewiseBookingReportAsync(
            startDate,
            endDate,
            customerId,
            productId,
            cancellationToken);

        return Ok(result);
    }
}
