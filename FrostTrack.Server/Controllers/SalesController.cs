namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Permission(ERoles.Admin)]
public class SalesController : ControllerBase
{
    private readonly ISalesService _salesService;

    public SalesController(ISalesService salesService)
    {
        _salesService = salesService;
    }

    [HttpGet]
    public async Task<IEnumerable<SalesListResponse>> GetSalesCategories(CancellationToken cancellationToken)
    {
        return await _salesService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<long>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<Sales, bool>> predicate = x => true;
        return await _salesService.GetLookup(predicate, cancellationToken);
    }


    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<SalesListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _salesService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SalesResponse>> GetSales(long id, CancellationToken cancellationToken)
    {

        var sales = await _salesService.GetByIdAsync(id, cancellationToken);
        if (sales == null)
        {
            return NotFound();
        }

        return sales;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SalesResponse>> PutSales(long id, SalesRequest sales)
    {
        var response = await _salesService.UpdateAsync(id, sales);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<SalesResponse>> PostSales(SalesRequest sales, CancellationToken cancellationToken)
    {
        return await _salesService.AddAsync(sales, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteSales(long id, CancellationToken cancellationToken)
    {
        return await _salesService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<long> ids, CancellationToken cancellationToken)
    {
        return await _salesService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsSalesExists")]
    public async ValueTask<bool> IsSalesExists([FromQuery] long id, CancellationToken cancellationToken)
    {
        var response = await _salesService.IsExistsAsync(id, cancellationToken);
        return response;
    }

    [HttpGet("generate-invoice-number")]
    public async ValueTask<CodeResponse> GenerateInvoiceNumber(CancellationToken cancellationToken)
    {
        var response = await _salesService.GenerateInvoiceNumber(cancellationToken);
        return new CodeResponse(response);
    }
}
