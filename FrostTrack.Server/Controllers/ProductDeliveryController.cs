using Application.Contractors;
using Application.Framework;
using Application.ReponseDTO;
using Application.RequestDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DeliveryController : ControllerBase
{
    private readonly IDeliveryService _service;

    public DeliveryController(IDeliveryService service)
    {
        _service = service;
    }

    [HttpGet("get-with-pagination")]
    public async Task<ActionResult<PaginationResult<DeliveryResponse>>> GetWithPagination([FromQuery] PaginationQuery query)
    {
        var result = await _service.GetWithPaginationAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DeliveryResponse>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<DeliveryResponse>> Create([FromBody] CreateDeliveryRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DeliveryResponse>> Update(Guid id, [FromBody] UpdateDeliveryRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return Ok(result);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> BatchDelete([FromBody] Guid[] ids)
    {
        var result = await _service.BatchDeleteAsync(ids);
        return Ok(result);
    }

    [HttpGet("generate-delivery-number")]
    public async Task<ActionResult<CodeResponse>> GenerateDeliveryNumber()
    {
        var number = await _service.GenerateDeliveryNumberAsync();
        return Ok(new CodeResponse { Code = number });
    }

    [HttpGet("booking/{bookingNumber}")]
    public async Task<ActionResult<BookingForDeliveryResponse>> GetBookingForDelivery(string bookingNumber)
    {
        var result = await _service.GetBookingForDeliveryAsync(bookingNumber);
        return Ok(result);
    }

    [HttpGet("remaining-quantities/{bookingId}")]
    public async Task<ActionResult<List<RemainingQuantityResponse>>> GetRemainingQuantities(Guid bookingId)
    {
        var result = await _service.GetRemainingQuantitiesAsync(bookingId);
        return Ok(result);
    }

    [HttpGet("booking-lookup")]
    public async Task<ActionResult<IEnumerable<Lookup<Guid>>>> GetBookingLookup()
    {
        var result = await _service.GetBookingLookupAsync();
        return Ok(result);
    }

    [HttpGet("booking-previous-payments/{bookingId}")]
    public async Task<ActionResult<decimal>> GetBookingPreviousPayments(Guid bookingId)
    {
        var result = await _service.GetBookingPreviousPaymentsAsync(bookingId);
        return Ok(result);
    }

    [HttpGet("booking-due-amount/{bookingId}")]
    public async Task<ActionResult<decimal>> GetBookingDueAmount(Guid bookingId)
    {
        var result = await _service.GetBookingDueAmountAsync(bookingId);
        return Ok(result);
    }

    [HttpGet("delivery-lookup")]
    public async Task<ActionResult<IEnumerable<Lookup<Guid>>>> GetDeliveryLookup()
    {
        var result = await _service.GetDeliveryLookupAsync();
        return Ok(result);
    }

    [HttpGet("invoice/{id}")]
    public async Task<ActionResult<DeliveryInvoiceResponse>> GetInvoiceById(Guid id)
    {
        var result = await _service.GetInvoiceByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("unpaid-by-customer/{customerId}")]
    public async Task<ActionResult<List<DeliveryResponse>>> GetUnpaidDeliveriesByCustomer(int customerId)
    {
        var result = await _service.GetUnpaidDeliveriesByCustomerAsync(customerId);
        return Ok(result);
    }

    [HttpGet("unpaid-by-code/{deliveryCode}")]
    public async Task<ActionResult<DeliveryResponse?>> GetUnpaidDeliveryByCode(string deliveryCode)
    {
        var result = await _service.GetUnpaidDeliveryByCodeAsync(deliveryCode);
        if (result == null)
            return NotFound(new { message = "Unpaid delivery not found" });
        return Ok(result);
    }

    [HttpGet("unpaid-all")]
    public async Task<ActionResult<List<DeliveryResponse>>> GetAllUnpaidDeliveries()
    {
        var result = await _service.GetAllUnpaidDeliveriesAsync();
        return Ok(result);
    }

    [HttpGet("by-transaction/{transactionId}")]
    public async Task<ActionResult<List<DeliveryResponse>>> GetDeliveriesByTransactionId(Guid transactionId)
    {
        var result = await _service.GetDeliveriesByTransactionIdAsync(transactionId);
        return Ok(result);
    }
}
