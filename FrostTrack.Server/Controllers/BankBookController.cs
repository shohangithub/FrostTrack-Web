using Application.Contractors;
using Application.ReponseDTO;
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
    public async Task<ActionResult<BankBookResponse>> GetBankBook([FromQuery] DateTime reportDate, [FromQuery] int? bankId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _bankBookService.GetBankBookAsync(reportDate, bankId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while generating bank book report.", error = ex.Message });
        }
    }

    [HttpGet("range")]
    public async Task<ActionResult<BankBookRangeResponse>> GetBankBookRange(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int? bankId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _bankBookService.GetBankBookRangeAsync(fromDate, toDate, bankId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while generating bank book range report.", error = ex.Message });
        }
    }
}
