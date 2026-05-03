namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Permission(ERoles.Admin)]
public class SupplierController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SupplierController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    [Route("get-list")]
    public async Task<IEnumerable<SupplierListResponse>> GetSuppliers(CancellationToken cancellationToken)
    {
        return await _supplierService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<int>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<Supplier, bool>> predicate = x => x.IsActive == x.IsActive;
        return await _supplierService.GetLookup(predicate, cancellationToken);
    }


    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<SupplierListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _supplierService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SupplierResponse>> GetProductCategories(short id, CancellationToken cancellationToken)
    {

        var supplier = await _supplierService.GetByIdAsync(id, cancellationToken);
        if (supplier == null)
        {
            return NotFound();
        }

        return supplier;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SupplierResponse>> PutSupplier(short id, SupplierRequest supplier)
    {
        var response = await _supplierService.UpdateAsync(id, supplier);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<SupplierResponse>> PostSupplier(SupplierRequest supplier, CancellationToken cancellationToken)
    {
        return await _supplierService.AddAsync(supplier, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteSupplier(short id, CancellationToken cancellationToken)
    {
        return await _supplierService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<int> ids, CancellationToken cancellationToken)
    {
        return await _supplierService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsSupplierExists")]
    public async ValueTask<bool> IsSupplierExists([FromQuery] short id, CancellationToken cancellationToken)
    {
        var response = await _supplierService.IsExistsAsync(id, cancellationToken);
        return response;
    }

    [HttpGet("generate-code")]
    public async ValueTask<CodeResponse> GenerateCode(CancellationToken cancellationToken)
    {
        var response = await _supplierService.GenerateCode(cancellationToken);
        return new CodeResponse(response);
    }

    [HttpGet("get-with-pagination-status")]
    public async Task<PaginationResult<SupplierListResponse>> GetWithPaginationStatus([FromQuery] SetupPaginationQuery requestQuery, CancellationToken cancellationToken)
        => await _supplierService.PaginationListAsync(requestQuery, cancellationToken);

    [HttpPost("{id}/soft-delete")]
    public async Task<IActionResult> SoftDelete(int id, CancellationToken cancellationToken)
        => Ok(await _supplierService.SoftDeleteAsync(id, cancellationToken));

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        => Ok(await _supplierService.RestoreAsync(id, cancellationToken));

    [HttpPost("{id}/archive")]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken)
        => Ok(await _supplierService.ArchiveAsync(id, cancellationToken));

    [HttpPost("{id}/unarchive")]
    public async Task<IActionResult> Unarchive(int id, CancellationToken cancellationToken)
        => Ok(await _supplierService.UnarchiveAsync(id, cancellationToken));

    [HttpDelete("{id}/permanent")]
    public async Task<IActionResult> PermanentDelete(int id, CancellationToken cancellationToken)
        => Ok(await _supplierService.DeleteAsync(id, cancellationToken));
}
