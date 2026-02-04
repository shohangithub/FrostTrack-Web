using Application.Contractors;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DatewiseDeliveryReportController : ControllerBase
{
    private readonly IDatewiseDeliveryReportService _datewiseDeliveryReportService;

    public DatewiseDeliveryReportController(IDatewiseDeliveryReportService datewiseDeliveryReportService)
    {
        _datewiseDeliveryReportService = datewiseDeliveryReportService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDatewiseDeliveryReport(
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


        var result = await _datewiseDeliveryReportService.GetDatewiseDeliveryReportAsync(
            startDate,
            endDate,
            customerId,
            productId,
            cancellationToken);

        return Ok(result);
    }
}
