using Application.Contractors;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CustomerBillStatementController : ControllerBase
{
    private readonly ICustomerBillStatementService _service;

    public CustomerBillStatementController(ICustomerBillStatementService service)
    {
        _service = service;
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetContractCustomers(CancellationToken cancellationToken)
        => Ok(await _service.GetContractCustomersAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> GetStatement(
        [FromQuery] int customerId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        if (customerId <= 0)
            return BadRequest(new { message = "Please select a customer" });
        if (fromDate.Date > toDate.Date)
            return BadRequest(new { message = "From date cannot be after To date" });

        // Dates arrive as local calendar days; convert the day boundaries to UTC
        var fromUtc = DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Local).ToUniversalTime();
        var toUtcExclusive = DateTime.SpecifyKind(toDate.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();

        var result = await _service.GetStatementAsync(customerId, fromUtc, toUtcExclusive, cancellationToken);
        return Ok(result);
    }
}
