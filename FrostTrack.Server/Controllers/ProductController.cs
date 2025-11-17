namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Permission(ERoles.Admin)]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [Route("get-list")]
    public async Task<IEnumerable<ProductListResponse>> GetProducts(CancellationToken cancellationToken)
    {
        return await _productService.ListAsync(cancellationToken: cancellationToken);
    }

    [HttpGet]
    [Route("get-list-without-service")]
    public async Task<IEnumerable<ProductListResponse>> GetProductsWithoutService(CancellationToken cancellationToken)
    {
        Expression<Func<Product, bool>> predicate = x => x.IsActive;
        return await _productService.ListAsync(predicate, cancellationToken);
    }


    [HttpGet]
    [Route("get-list-with-stock")]
    public async Task<IEnumerable<ProductLisWithStockResponse>> GetProductWithStock(CancellationToken cancellationToken)
    {
        return await _productService.ListwithStockAsync(cancellationToken);
    }


    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<int>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<Product, bool>> predicate = x => x.IsActive == x.IsActive;
        return await _productService.GetLookup(predicate, cancellationToken);
    }


    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<ProductListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _productService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponse>> GetProductCategories(short id, CancellationToken cancellationToken)
    {

        var product = await _productService.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            return NotFound();
        }

        return product;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductResponse>> PutProduct(short id, ProductRequest product)
    {
        var response = await _productService.UpdateAsync(id, product);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> PostProduct(ProductRequest product, CancellationToken cancellationToken)
    {
        return await _productService.AddAsync(product, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteProduct(short id, CancellationToken cancellationToken)
    {
        return await _productService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<int> ids, CancellationToken cancellationToken)
    {
        return await _productService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsProductExists")]
    public async ValueTask<bool> IsProductExists([FromQuery] short id, CancellationToken cancellationToken)
    {
        var response = await _productService.IsExistsAsync(id, cancellationToken);
        return response;
    }

    [HttpGet("generate-code")]
    public async ValueTask<CodeResponse> GenerateCode(CancellationToken cancellationToken)
    {
        var response = await _productService.GenerateCode(cancellationToken);
        return new CodeResponse(response);
    }
}
