using Application.Contractors;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DailyStockBookController : ControllerBase
{
    private readonly IDailyStockBookService _dailyStockBookService;

    public DailyStockBookController(IDailyStockBookService dailyStockBookService)
    {
        _dailyStockBookService = dailyStockBookService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDailyStockBook(
        [FromQuery] DateTime? reportDate,
        [FromQuery] int? customerId,
        [FromQuery] int? productId,
        CancellationToken cancellationToken)
    {
        // Default to today if no date provided
         var dates = reportDate.Value.Date.ToUniversalTime();
       
        var result = await _dailyStockBookService.GetDailyStockBookAsync(
            dates,
            customerId,
            productId,
            cancellationToken);

        return Ok(result);
    }
}
