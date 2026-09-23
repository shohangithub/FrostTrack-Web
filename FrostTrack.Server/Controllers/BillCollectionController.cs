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
}
