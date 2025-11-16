namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Permission(ERoles.Admin)]
public class BaseUnitController : ControllerBase
{
    private readonly IBaseUnitService _baseBaseUnitService;

    public BaseUnitController(IBaseUnitService baseBaseUnitService)
    {
        _baseBaseUnitService = baseBaseUnitService;
    }

    [HttpGet]
    public async Task<IEnumerable<BaseUnitListResponse>> GetProductCategories(CancellationToken cancellationToken)
    {
        return await _baseBaseUnitService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<int>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<BaseUnit, bool>> predicate = x => x.IsActive == x.IsActive;
        return await _baseBaseUnitService.GetLookup(predicate, cancellationToken);
    }


    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<BaseUnitListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _baseBaseUnitService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BaseUnitResponse>> GetProductCategories(short id, CancellationToken cancellationToken)
    {

        var baseBaseUnit = await _baseBaseUnitService.GetByIdAsync(id, cancellationToken);
        if (baseBaseUnit == null)
        {
            return NotFound();
        }

        return baseBaseUnit;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BaseUnitResponse>> PutBaseUnit(short id, BaseUnitRequest baseBaseUnit)
    {
        var response = await _baseBaseUnitService.UpdateAsync(id, baseBaseUnit);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<BaseUnitResponse>> PostBaseUnit(BaseUnitRequest baseBaseUnit, CancellationToken cancellationToken)
    {
        return await _baseBaseUnitService.AddAsync(baseBaseUnit, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteBaseUnit(short id, CancellationToken cancellationToken)
    {
        return await _baseBaseUnitService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<int> ids, CancellationToken cancellationToken)
    {
        return await _baseBaseUnitService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsBaseUnitExists")]
    public async ValueTask<bool> IsBaseUnitExists([FromQuery] short id, CancellationToken cancellationToken)
    {
        var response = await _baseBaseUnitService.IsExistsAsync(id, cancellationToken);
        return response;
    }
}
