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

    [HttpGet("generate-code")]
    public async Task<IActionResult> GenerateCode(CancellationToken cancellationToken)
    {
        var code = await _bookingService.GenerateBookingNumber(cancellationToken);
        return Ok(new Application.ReponseDTO.CodeResponse { Code = code });
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
    public async Task<PaginationResult<BookingListResponse>> GetWithPagination([FromQuery] BookingPaginationQuery requestQuery, CancellationToken cancellationToken)
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

    [HttpPost("{id}/soft-delete")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bookingService.SoftDeleteAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bookingService.RestoreAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bookingService.ArchiveAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/unarchive")]
    public async Task<IActionResult> Unarchive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bookingService.UnarchiveAsync(id, cancellationToken);
        return Ok(result);
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

    [HttpGet("invoice-with-delivery/{id}")]
    public async Task<ActionResult<BookingInvoiceWithDeliveryResponse>> GetInvoiceWithDelivery(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _bookingService.GetInvoiceWithDeliveryAsync(id, cancellationToken);
        if (invoice == null)
        {
            return NotFound();
        }

        return invoice;
    }

    [HttpGet("customer-due-summary")]
    public async Task<ActionResult<IEnumerable<CustomerDueSummaryResponse>>> GetCustomerDueSummary(CancellationToken cancellationToken)
    {
        var dueSummary = await _bookingService.GetCustomerDueSummaryAsync(cancellationToken);
        return Ok(dueSummary);
    }

    [HttpGet("customer-due-detail/{customerId}")]
    public async Task<ActionResult<IEnumerable<CustomerDueDetailResponse>>> GetCustomerDueDetail(int customerId, CancellationToken cancellationToken)
    {
        var dueDetail = await _bookingService.GetCustomerDueDetailAsync(customerId, cancellationToken);
        return Ok(dueDetail);
    }

    [HttpGet("customer-outstanding/{customerId}")]
    public async Task<ActionResult<CustomerOutstandingResponse>> GetCustomerOutstanding(int customerId, CancellationToken cancellationToken)
    {
        var outstanding = await _bookingService.GetCustomerOutstandingAsync(customerId, cancellationToken);
        return Ok(outstanding);
    }
}
