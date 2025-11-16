namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Permission(ERoles.Admin)]
public class PurchaseReportController : ControllerBase
{
    private readonly IPurchaseReportService _purchaseService;

    public PurchaseReportController(IPurchaseReportService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    // [HttpGet]
    // public async Task<IEnumerable<PurchaseListResponse>> GetPurchaseCategories(CancellationToken cancellationToken)
    // {
    //     return await _purchaseService.ListAsync(cancellationToken);
    // }

    // [HttpGet]
    // [Route("Lookup")]
    // public async Task<IEnumerable<Lookup<long>>> GetLookup(CancellationToken cancellationToken)
    // {
    //     Expression<Func<Purchase, bool>> predicate = x => true;
    //     return await _purchaseService.GetLookup(predicate, cancellationToken);
    // }


    // [HttpGet]
    // [Route("get-with-pagination")]
    // public async Task<PaginationResult<PurchaseListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    // {
    //     return await _purchaseService.PaginationListAsync(requestQuery, cancellationToken);
    // }

    [HttpGet("get-invoice/{id}")]
    public async Task<ActionResult<PurchaseInvoiceReportResponse>> GetInvoice(long id, CancellationToken cancellationToken)
    {

        var purchase = await _purchaseService.GetPurchaseInvoiceAsync(id, cancellationToken);
        if (purchase == null)
        {
            return NotFound();
        }

        return purchase;
    }

    // [HttpPut("{id}")]
    // public async Task<ActionResult<PurchaseResponse>> PutPurchase(long id, PurchaseRequest purchase)
    // {
    //     var response = await _purchaseService.UpdateAsync(id, purchase);
    //     return response;
    // }

    // [HttpPost]
    // public async Task<ActionResult<PurchaseResponse>> PostPurchase(PurchaseRequest purchase, CancellationToken cancellationToken)
    // {
    //     return await _purchaseService.AddAsync(purchase, cancellationToken);
    // }

    // [HttpDelete("{id}")]
    // public async ValueTask<bool> DeletePurchase(long id, CancellationToken cancellationToken)
    // {
    //     return await _purchaseService.DeleteAsync(id, cancellationToken);
    // }

    // [HttpPost("DeleteBatch")]
    // public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<long> ids, CancellationToken cancellationToken)
    // {
    //     return await _purchaseService.DeleteBatchAsync(ids, cancellationToken);
    // }

    // [HttpGet("IsPurchaseExists")]
    // public async ValueTask<bool> IsPurchaseExists([FromQuery] long id, CancellationToken cancellationToken)
    // {
    //     var response = await _purchaseService.IsExistsAsync(id, cancellationToken);
    //     return response;
    // }

    // [HttpGet("generate-invoice-number")]
    // public async ValueTask<CodeResponse> GenerateInvoiceNumber(CancellationToken cancellationToken)
    // {
    //     var response = await _purchaseService.GenerateInvoiceNumber(cancellationToken);
    //     return new CodeResponse(response);
    // }
}
