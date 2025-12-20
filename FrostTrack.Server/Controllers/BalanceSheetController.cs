using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

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
        var result = await _service.GetBalanceSheetAsync(reportDate, cancellationToken);
        return Ok(result);
    }
}
