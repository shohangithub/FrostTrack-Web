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
    /// Get balance sheet report as of a specific date
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<BalanceSheetSummaryResponse>> GetBalanceSheet(
        [FromQuery] DateTime asOfDate,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetBalanceSheetAsync(asOfDate, branchId, cancellationToken);
        return Ok(result);
    }
}
