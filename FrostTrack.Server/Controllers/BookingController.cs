namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Permission(ERoles.Admin)]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<IEnumerable<BookingListResponse>> GetBookings(CancellationToken cancellationToken)
    {
        return await _bookingService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<Guid>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<Booking, bool>> predicate = x => true;
        return await _bookingService.GetLookup(predicate, cancellationToken);
    }

    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<BookingListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _bookingService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookingResponse>> GetBooking(Guid id, CancellationToken cancellationToken)
    {
        var booking = await _bookingService.GetByIdAsync(id, cancellationToken);
        if (booking == null)
        {
            return NotFound();
        }

        return booking;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BookingResponse>> PutBooking(Guid id, BookingRequest booking)
    {
        var response = await _bookingService.UpdateAsync(id, booking);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponse>> PostBooking(BookingRequest booking, CancellationToken cancellationToken)
    {
        return await _bookingService.AddAsync(booking, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteBooking(Guid id, CancellationToken cancellationToken)
    {
        return await _bookingService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<Guid> ids, CancellationToken cancellationToken)
    {
        return await _bookingService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsBookingExists")]
    public async ValueTask<bool> IsBookingExists([FromQuery] Guid id, CancellationToken cancellationToken)
    {
        var response = await _bookingService.IsExistsAsync(id, cancellationToken);
        return response;
    }

    [HttpGet("generate-booking-number")]
    public async ValueTask<CodeResponse> GenerateBookingNumber(CancellationToken cancellationToken)
    {
        var response = await _bookingService.GenerateBookingNumber(cancellationToken);
        return new CodeResponse(response);
    }
}
