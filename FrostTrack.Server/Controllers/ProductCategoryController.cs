namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Authorize(Policy = "RequireSeller")] // Use policy-based authorization

[Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Admin}")]
public class ProductCategoryController : ControllerBase
{
    private readonly IProductCategoryService _productCategoryService;

    public ProductCategoryController(IProductCategoryService productCategoryService)
    {
        _productCategoryService = productCategoryService;
    }

    [HttpGet]
    public async Task<IEnumerable<ProductCategoryListResponse>> GetProductCategories(CancellationToken cancellationToken)
    {
        return await _productCategoryService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<int>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<ProductCategory, bool>> predicate = x => x.IsActive == x.IsActive;
        return await _productCategoryService.GetLookup(predicate, cancellationToken);
    }


    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<ProductCategoryListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _productCategoryService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductCategoryResponse>> GetProductCategories(int id, CancellationToken cancellationToken)
    {

        var productCategory = await _productCategoryService.GetByIdAsync(id, cancellationToken);
        if (productCategory == null)
        {
            return NotFound();
        }

        return productCategory;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductCategoryResponse>> PutProductCategory(int id, ProductCategoryRequest productCategory)
    {
        var response = await _productCategoryService.UpdateAsync(id, productCategory);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<ProductCategoryResponse>> PostProductCategory(ProductCategoryRequest productCategory, CancellationToken cancellationToken)
    {
        return await _productCategoryService.AddAsync(productCategory, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteProductCategory(int id, CancellationToken cancellationToken)
    {
        return await _productCategoryService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<int> ids, CancellationToken cancellationToken)
    {
        return await _productCategoryService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsProductCategoryExists")]
    public async ValueTask<bool> IsProductCategoryExists([FromQuery] int id, CancellationToken cancellationToken)
    {
        var response = await _productCategoryService.IsExistsAsync(id, cancellationToken);
        return response;
    }

    [HttpGet("get-with-pagination-status")]
    public async Task<PaginationResult<ProductCategoryListResponse>> GetWithPaginationStatus([FromQuery] SetupPaginationQuery requestQuery, CancellationToken cancellationToken)
        => await _productCategoryService.PaginationListAsync(requestQuery, cancellationToken);

    [HttpPost("{id}/soft-delete")]
    public async Task<IActionResult> SoftDelete(int id, CancellationToken cancellationToken)
        => Ok(await _productCategoryService.SoftDeleteAsync(id, cancellationToken));

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => Ok(await _productCategoryService.RestoreAsync(id, cancellationToken));

    [HttpPost("{id}/archive")]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken)
        => Ok(await _productCategoryService.ArchiveAsync(id, cancellationToken));

    [HttpPost("{id}/unarchive")]
    public async Task<IActionResult> Unarchive(int id, CancellationToken cancellationToken)
        => Ok(await _productCategoryService.UnarchiveAsync(id, cancellationToken));

    [HttpDelete("{id}/permanent")]
    public async Task<IActionResult> PermanentDelete(int id, CancellationToken cancellationToken)
        => Ok(await _productCategoryService.DeleteAsync(id, cancellationToken));
}
