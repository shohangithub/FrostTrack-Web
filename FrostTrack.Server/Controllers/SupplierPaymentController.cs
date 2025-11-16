namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Permission(ERoles.Admin)]
public class SupplierPaymentController : ControllerBase
{
    private readonly ISupplierPaymentService _supplierPaymentService;

    public SupplierPaymentController(ISupplierPaymentService supplierPaymentService)
    {
        _supplierPaymentService = supplierPaymentService;
    }

    [HttpGet]
    public async Task<IEnumerable<SupplierPaymentListResponse>> GetSupplierPayments(CancellationToken cancellationToken)
    {
        return await _supplierPaymentService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<long>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<SupplierPayment, bool>> predicate = x => true;
        return await _supplierPaymentService.GetLookup(predicate, cancellationToken);
    }

    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<SupplierPaymentListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _supplierPaymentService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SupplierPaymentResponse>> GetSupplierPayment(long id, CancellationToken cancellationToken)
    {
        var payment = await _supplierPaymentService.GetByIdAsync(id, cancellationToken);
        if (payment == null)
        {
            return NotFound();
        }

        return payment;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SupplierPaymentResponse>> PutSupplierPayment(long id, SupplierPaymentRequest payment)
    {
        var response = await _supplierPaymentService.UpdateAsync(id, payment);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<SupplierPaymentResponse>> PostSupplierPayment(SupplierPaymentRequest payment, CancellationToken cancellationToken)
    {
        return await _supplierPaymentService.AddAsync(payment, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteSupplierPayment(long id, CancellationToken cancellationToken)
    {
        return await _supplierPaymentService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<long> ids, CancellationToken cancellationToken)
    {
        return await _supplierPaymentService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsSupplierPaymentExists")]
    public async ValueTask<bool> IsSupplierPaymentExists([FromQuery] long id, CancellationToken cancellationToken)
    {
        var response = await _supplierPaymentService.IsExistsAsync(id, cancellationToken);
        return response;
    }

    [HttpGet("generate-payment-number")]
    public async ValueTask<CodeResponse> GeneratePaymentNumber(CancellationToken cancellationToken)
    {
        var response = await _supplierPaymentService.GeneratePaymentNumber(cancellationToken);
        return new CodeResponse(response);
    }

    [HttpGet("supplier-due-balance/{supplierId}")]
    public async Task<decimal> GetSupplierDueBalance(int supplierId, CancellationToken cancellationToken)
    {
        return await _supplierPaymentService.GetSupplierDueBalance(supplierId, cancellationToken);
    }

    [HttpGet("pending-purchases/{supplierId}")]
    public async Task<IEnumerable<PurchaseListResponse>> GetPendingPurchases(int supplierId, CancellationToken cancellationToken)
    {
        return await _supplierPaymentService.GetPendingPurchases(supplierId, cancellationToken);
    }

    [HttpGet("pending-sales/{customerId}")]
    public async Task<IEnumerable<SalesListResponse>> GetPendingSales(int customerId, CancellationToken cancellationToken)
    {
        return await _supplierPaymentService.GetPendingSales(customerId, cancellationToken);
    }
}