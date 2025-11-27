using Application.Contractors;
using Application.ReponseDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StockReportController : ControllerBase
{
    private readonly IStockReportService _stockReportService;

    public StockReportController(IStockReportService stockReportService)
    {
        _stockReportService = stockReportService;
    }

    /// <summary>
    /// Get stock report with optional filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StockReportItemResponse>>> GetStockReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? customerId = null,
        [FromQuery] int? productId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _stockReportService.GetStockReportAsync(
            startDate,
            endDate,
            customerId,
            productId,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get stock report grouped by customer
    /// </summary>
    [HttpGet("by-customer")]
    public async Task<ActionResult<IEnumerable<CustomerStockReportResponse>>> GetCustomerStockReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var result = await _stockReportService.GetCustomerStockReportAsync(
            startDate,
            endDate,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get stock report grouped by product
    /// </summary>
    [HttpGet("by-product")]
    public async Task<ActionResult<IEnumerable<ProductStockReportResponse>>> GetProductStockReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var result = await _stockReportService.GetProductStockReportAsync(
            startDate,
            endDate,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get stock summary with aggregated totals
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<StockSummaryResponse>> GetStockSummary(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var result = await _stockReportService.GetStockSummaryAsync(
            startDate,
            endDate,
            cancellationToken);

        return Ok(result);
    }
}
