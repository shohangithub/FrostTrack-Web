using Application.Contractors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class BankBookController : ControllerBase
{
    private readonly IBankBookService _bankBookService;

    public BankBookController(IBankBookService bankBookService)
    {
        _bankBookService = bankBookService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBankBook([FromQuery] DateTime reportDate, CancellationToken cancellationToken)
    {
        var result = await _bankBookService.GetBankBookAsync(reportDate, cancellationToken);
        return Ok(result);
    }
}
