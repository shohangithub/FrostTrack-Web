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
            endDate ??= DateTime.Today.AddDays(1).AddSeconds(-1);
            startDate = endDate.Value.AddDays(-periodDays.Value).Date;
        }
        else
        {
            // Default to last 30 days if no parameters provided
            endDate ??= DateTime.Today.AddDays(1).AddSeconds(-1);
            startDate ??= endDate.Value.AddDays(-30).Date;
        }

        var stats = await _dashboardService.GetDashboardStatsAsync(
            startDate.Value,
            endDate.Value,
            branchId,
            cancellationToken);

        return Ok(stats);
    }

    [HttpGet("trends")]
    public async Task<IActionResult> GetDashboardTrends(
        [FromQuery] int? periodDays,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        // Default to 30 days if not provided
        var days = periodDays ?? 30;

        var trends = await _dashboardService.GetDashboardTrendsAsync(
            days,
            branchId,
            cancellationToken);

        return Ok(trends);
    }
}
