using Application.Contractors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class GeneralLedgerController : ControllerBase
{
    private readonly IGeneralLedgerService _generalLedgerService;

    public GeneralLedgerController(IGeneralLedgerService generalLedgerService)
    {
        _generalLedgerService = generalLedgerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetGeneralLedger([FromQuery] DateTime reportDate, CancellationToken cancellationToken)
    {
        var result = await _generalLedgerService.GetGeneralLedgerAsync(reportDate, cancellationToken);
        return Ok(result);
    }
}
