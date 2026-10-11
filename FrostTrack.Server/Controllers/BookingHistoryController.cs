namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BookingHistoryController : ControllerBase
{
    private readonly IBookingHistoryService _bookingHistoryService;

    public BookingHistoryController(IBookingHistoryService bookingHistoryService)
    {
        _bookingHistoryService = bookingHistoryService;
    }

    /// <summary>Full history of one booking: info, items, deliveries, statement with running due, adjustments, summary.</summary>
    [HttpGet("{bookingId}")]
    public async Task<ActionResult<BookingHistoryResponse>> GetHistory(Guid bookingId, CancellationToken cancellationToken)
        => Ok(await _bookingHistoryService.GetHistoryAsync(bookingId, cancellationToken));
}
