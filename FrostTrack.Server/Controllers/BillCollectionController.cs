using Application.Contractors;
using Application.ReponseDTO;
using Application.RequestDTO;
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

    [HttpGet("customer-balance/{customerId}")]
    public async Task<ActionResult<CustomerBalanceSummaryResponse>> GetCustomerBalance(
        int customerId,
        CancellationToken cancellationToken)
    {
        var response = await _billCollectionService.GetCustomerBalanceSummaryAsync(customerId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("customer-payment")]
    public async Task<ActionResult<TransactionResponse>> CreateCustomerPayment(
        CustomerPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _billCollectionService.CreateCustomerPaymentAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("customer/{customerId}/due-bookings")]
    public async Task<ActionResult<CustomerDueBookingsResponse>> GetCustomerDueBookings(int customerId, CancellationToken cancellationToken)
        => Ok(await _billCollectionService.GetCustomerDueBookingsAsync(customerId, cancellationToken));

    [HttpGet("booking/{bookingId}/payment-detail")]
    public async Task<ActionResult<BookingPaymentDetailResponse>> GetBookingPaymentDetail(Guid bookingId, CancellationToken cancellationToken)
        => Ok(await _billCollectionService.GetBookingPaymentDetailAsync(bookingId, cancellationToken));

    [HttpPost("booking-payment")]
    public async Task<ActionResult<BookingPaymentResultResponse>> CreateBookingPayment(BookingPaymentRequest request, CancellationToken cancellationToken)
        => Ok(await _billCollectionService.CreateBookingPaymentAsync(request, cancellationToken));

    [HttpGet("customer/{customerId}/advances")]
    public async Task<ActionResult<CustomerAdvancesResponse>> GetCustomerAdvances(int customerId, CancellationToken cancellationToken)
        => Ok(await _billCollectionService.GetCustomerAdvancesAsync(customerId, cancellationToken));

    [HttpDelete("advance-allocation/{allocationId}")]
    public async Task<ActionResult<bool>> RemoveAdvanceAllocation(Guid allocationId, CancellationToken cancellationToken)
        => Ok(await _billCollectionService.RemoveAdvanceAllocationAsync(allocationId, cancellationToken));

    [HttpGet("receipt-info/{transactionId}")]
    public async Task<ActionResult<PaymentReceiptInfoResponse>> GetPaymentReceiptInfo(Guid transactionId, CancellationToken cancellationToken)
        => Ok(await _billCollectionService.GetPaymentReceiptInfoAsync(transactionId, cancellationToken));

    [HttpPost("delivery-based")]
    public async Task<ActionResult<TransactionResponse>> CreateDeliveryBillCollection(
        DeliveryBillCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _billCollectionService.CreateDeliveryBillCollectionAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("report")]
    public async Task<ActionResult<List<CustomerPaymentReportItemResponse>>> GetCustomerPaymentReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? customerId,
        [FromQuery] string? paymentMethod,
        CancellationToken cancellationToken)
    {
        var response = await _billCollectionService.GetCustomerPaymentReportAsync(startDate, endDate, customerId, paymentMethod, cancellationToken);
        return Ok(response);
    }

    [HttpPost("discount")]
    public async Task<ActionResult<TransactionResponse>> CreateCustomerDiscount(
        CustomerDiscountRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _billCollectionService.CreateCustomerDiscountAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("customer/{customerId}/discount-history")]
    public async Task<ActionResult<List<CustomerDiscountItemResponse>>> GetCustomerDiscountHistory(
        int customerId,
        CancellationToken cancellationToken)
    {
        var response = await _billCollectionService.GetCustomerDiscountHistoryAsync(customerId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("discount-report")]
    public async Task<ActionResult<List<CustomerDiscountItemResponse>>> GetCustomerDiscountReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? customerId,
        [FromQuery] string? searchTerm,
        CancellationToken cancellationToken)
    {
        var response = await _billCollectionService.GetCustomerDiscountReportAsync(startDate, endDate, customerId, searchTerm, cancellationToken);
        return Ok(response);
    }
}
