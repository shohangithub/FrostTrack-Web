using Application.Contractors;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DatewiseActivityReportController : ControllerBase
{
    private readonly IDatewiseActivityReportService _service;

    public DatewiseActivityReportController(IDatewiseActivityReportService service)
    {
        _service = service;
    }

    /// <param name="type">booking | delivery | both</param>
    [HttpGet]
    public async Task<IActionResult> GetReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int? customerId,
        [FromQuery] string type = "both",
        CancellationToken cancellationToken = default)
    {
        if (fromDate.Date > toDate.Date)
            return BadRequest(new { message = "From date cannot be after To date" });

        var normalized = (type ?? "both").ToLowerInvariant();
        var includeBookings = normalized is "booking" or "both";
        var includeDeliveries = normalized is "delivery" or "both";
        if (!includeBookings && !includeDeliveries)
            return BadRequest(new { message = "type must be booking, delivery or both" });

        var result = await _service.GetReportAsync(
            fromDate, toDate, customerId > 0 ? customerId : null,
            includeBookings, includeDeliveries, cancellationToken);
        return Ok(result);
    }
}
