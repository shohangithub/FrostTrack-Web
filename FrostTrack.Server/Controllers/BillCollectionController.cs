using Application.Contractors;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillCollectionController : ControllerBase
{
    private readonly IBillCollectionService _billCollectionService;

    public BillCollectionController(IBillCollectionService billCollectionService)
    {
        _billCollectionService = billCollectionService;
    }

    [HttpGet("bookings-with-due")]
    public async Task<IActionResult> GetBookingsWithDue(CancellationToken cancellationToken)
    {
        var bookings = await _billCollectionService.GetBookingsWithDueAsync(cancellationToken);
        return Ok(bookings);
    }

    [HttpGet("booking/{bookingId}")]
    public async Task<IActionResult> GetBookingForBillCollection(
        string bookingId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(bookingId, out var id))
        {
            return BadRequest(new { message = "Invalid booking ID format" });
        }

        var booking = await _billCollectionService.GetBookingForBillCollectionAsync(
            id,
            cancellationToken);

        if (booking == null)
        {
            return NotFound(new { message = "Booking not found" });
        }

        return Ok(booking);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> CreateBillCollection(
        BillCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _billCollectionService.CreateBillCollectionAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TransactionResponse>> UpdateBillCollection(
        Guid id,
        BillCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _billCollectionService.UpdateBillCollectionAsync(id, request, cancellationToken);
        return Ok(response);
    }
}
