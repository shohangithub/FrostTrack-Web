using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

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
    /// Get trial balance report with date range filter
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<TrialBalanceSummaryResponse>> GetTrialBalance(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetTrialBalanceAsync(startDate, endDate, branchId, cancellationToken);
        return Ok(result);
    }
}
