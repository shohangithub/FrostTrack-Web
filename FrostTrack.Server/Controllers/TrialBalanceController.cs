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
    /// Get trial balance report for a specific date
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<TrialBalanceSummaryResponse>> GetTrialBalance(
        [FromQuery] DateTime reportDate,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetTrialBalanceAsync(reportDate, cancellationToken);
        return Ok(result);
    }
}
