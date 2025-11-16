namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Permission(ERoles.Admin)]
public class SaleReturnController : ControllerBase
{
    private readonly ISaleReturnService _saleReturnService;

    public SaleReturnController(ISaleReturnService saleReturnService)
    {
        _saleReturnService = saleReturnService;
    }

    [HttpGet]
    public async Task<IEnumerable<SaleReturnListResponse>> GetSaleReturns(CancellationToken cancellationToken)
    {
        return await _saleReturnService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<long>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<SaleReturn, bool>> predicate = x => true;
        return await _saleReturnService.GetLookup(predicate, cancellationToken);
    }

    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<SaleReturnListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _saleReturnService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SaleReturnResponse>> GetSaleReturn(long id, CancellationToken cancellationToken)
    {
        var saleReturn = await _saleReturnService.GetByIdAsync(id, cancellationToken);
        if (saleReturn == null)
        {
            return NotFound();
        }

        return saleReturn;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SaleReturnResponse>> PutSaleReturn(long id, SaleReturnRequest saleReturn)
    {
        var response = await _saleReturnService.UpdateAsync(id, saleReturn);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<SaleReturnResponse>> PostSaleReturn(SaleReturnRequest saleReturn, CancellationToken cancellationToken)
    {
        return await _saleReturnService.AddAsync(saleReturn, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteSaleReturn(long id, CancellationToken cancellationToken)
    {
        return await _saleReturnService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<long> ids, CancellationToken cancellationToken)
    {
        return await _saleReturnService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsSaleReturnExists")]
    public async ValueTask<bool> IsSaleReturnExists([FromQuery] long id, CancellationToken cancellationToken)
    {
        var response = await _saleReturnService.IsExistsAsync(id, cancellationToken);
        return response;
    }

    [HttpGet("generate-return-number")]
    public async ValueTask<CodeResponse> GenerateReturnNumber(CancellationToken cancellationToken)
    {
        var response = await _saleReturnService.GenerateReturnNumber(cancellationToken);
        return new CodeResponse(response);
    }
}