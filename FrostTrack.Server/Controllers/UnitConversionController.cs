namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Permission(ERoles.Admin)]
public class UnitConversionController : ControllerBase
{
    private readonly IUnitConversionService _unitConversionService;

    public UnitConversionController(IUnitConversionService unitConversionService)
    {
        _unitConversionService = unitConversionService;
    }

    [HttpGet]
    public async Task<IEnumerable<UnitConversionListResponse>> GetProductCategories(CancellationToken cancellationToken)
    {
        return await _unitConversionService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<int>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<UnitConversion, bool>> predicate = x => x.IsActive == x.IsActive;
        return await _unitConversionService.GetLookup(predicate, cancellationToken);
    }


    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<UnitConversionListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _unitConversionService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UnitConversionResponse>> GetProductCategories(short id, CancellationToken cancellationToken)
    {

        var baseUnitConversion = await _unitConversionService.GetByIdAsync(id, cancellationToken);
        if (baseUnitConversion == null)
        {
            return NotFound();
        }

        return baseUnitConversion;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UnitConversionResponse>> PutUnitConversion(short id, UnitConversionRequest baseUnitConversion)
    {
        var response = await _unitConversionService.UpdateAsync(id, baseUnitConversion);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<UnitConversionResponse>> PostUnitConversion(UnitConversionRequest baseUnitConversion, CancellationToken cancellationToken)
    {
        return await _unitConversionService.AddAsync(baseUnitConversion, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteUnitConversion(short id, CancellationToken cancellationToken)
    {
        return await _unitConversionService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<int> ids, CancellationToken cancellationToken)
    {
        return await _unitConversionService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsUnitConversionExists")]
    public async ValueTask<bool> IsUnitConversionExists([FromQuery] short id, CancellationToken cancellationToken)
    {
        var response = await _unitConversionService.IsExistsAsync(id, cancellationToken);
        return response;
    }
}
