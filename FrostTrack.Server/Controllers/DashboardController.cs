using Application.Contractors;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardStats(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? branchId,
        [FromQuery] int? periodDays,
        CancellationToken cancellationToken)
    {
        // If periodDays is provided, calculate startDate from endDate or today
        if (periodDays.HasValue)
        {
            endDate ??= DateTime.UtcNow;
            startDate = endDate.Value.AddDays(-periodDays.Value);
        }
        else
        {
            // Default to last 30 days if no parameters provided
            endDate ??= DateTime.UtcNow;
            startDate ??= endDate.Value.AddDays(-30);
        }

        var stats = await _dashboardService.GetDashboardStatsAsync(
            startDate.Value,
            endDate.Value,
            branchId,
            cancellationToken);

        return Ok(stats);
    }
}
