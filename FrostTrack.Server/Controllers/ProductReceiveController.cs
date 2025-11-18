namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Permission(ERoles.Admin)]
public class ProductReceiveController : ControllerBase
{
    private readonly IProductReceiveService _productReceiveService;

    public ProductReceiveController(IProductReceiveService productReceiveService)
    {
        _productReceiveService = productReceiveService;
    }

    [HttpGet]
    public async Task<IEnumerable<ProductReceiveListResponse>> GetProductReceives(CancellationToken cancellationToken)
    {
        return await _productReceiveService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<long>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<Booking, bool>> predicate = x => true;
        return await _productReceiveService.GetLookup(predicate, cancellationToken);
    }

    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<ProductReceiveListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _productReceiveService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductReceiveResponse>> GetProductReceive(long id, CancellationToken cancellationToken)
    {
        var productReceive = await _productReceiveService.GetByIdAsync(id, cancellationToken);
        if (productReceive == null)
        {
            return NotFound();
        }

        return productReceive;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductReceiveResponse>> PutProductReceive(long id, ProductReceiveRequest productReceive)
    {
        var response = await _productReceiveService.UpdateAsync(id, productReceive);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<ProductReceiveResponse>> PostProductReceive(ProductReceiveRequest productReceive, CancellationToken cancellationToken)
    {
        return await _productReceiveService.AddAsync(productReceive, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteProductReceive(long id, CancellationToken cancellationToken)
    {
        return await _productReceiveService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<long> ids, CancellationToken cancellationToken)
    {
        return await _productReceiveService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsProductReceiveExists")]
    public async ValueTask<bool> IsProductReceiveExists([FromQuery] long id, CancellationToken cancellationToken)
    {
        var response = await _productReceiveService.IsExistsAsync(id, cancellationToken);
        return response;
    }

    [HttpGet("generate-receive-number")]
    public async ValueTask<CodeResponse> GenerateReceiveNumber(CancellationToken cancellationToken)
    {
        var response = await _productReceiveService.GenerateReceiveNumber(cancellationToken);
        return new CodeResponse(response);
    }
}
