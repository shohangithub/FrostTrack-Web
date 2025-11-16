namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Permission(ERoles.Admin)]
public class AssetController : ControllerBase
{
    private readonly IAssetService _assetService;

    public AssetController(IAssetService assetService)
    {
        _assetService = assetService;
    }

    [HttpGet]
    [Route("get-list")]
    public async Task<IEnumerable<AssetListResponse>> GetAssets(CancellationToken cancellationToken)
    {
        return await _assetService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<int>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<Asset, bool>> predicate = x => x.IsActive == true;
        return await _assetService.GetLookup(predicate, cancellationToken);
    }

    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<AssetListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _assetService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AssetResponse>> GetAsset(int id, CancellationToken cancellationToken)
    {
        var asset = await _assetService.GetByIdAsync(id, cancellationToken);
        if (asset == null)
        {
            return NotFound();
        }

        return asset;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AssetResponse>> PutAsset(int id, AssetRequest asset)
    {
        var response = await _assetService.UpdateAsync(id, asset);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<AssetResponse>> PostAsset(AssetRequest asset, CancellationToken cancellationToken)
    {
        return await _assetService.AddAsync(asset, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteAsset(int id, CancellationToken cancellationToken)
    {
        return await _assetService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<int> ids, CancellationToken cancellationToken)
    {
        return await _assetService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsAssetExists")]
    public async ValueTask<bool> IsAssetExists([FromQuery] int id, CancellationToken cancellationToken)
    {
        var response = await _assetService.IsExistsAsync(id, cancellationToken);
        return response;
    }

    [HttpGet("generate-code")]
    public async Task<ActionResult<CodeResponse>> GenerateCode(CancellationToken cancellationToken)
    {
        var code = await _assetService.GenerateCode(cancellationToken);
        return new CodeResponse(code);
    }

    [HttpGet("{id}/current-value")]
    public async Task<ActionResult<decimal>> GetCurrentValue(int id, CancellationToken cancellationToken)
    {
        var currentValue = await _assetService.GetCurrentValueAsync(id, cancellationToken);
        return currentValue;
    }

    [HttpGet("asset-types")]
    public async Task<ActionResult<IEnumerable<string>>> GetDistinctAssetTypes(CancellationToken cancellationToken)
    {
        var assetTypes = await _assetService.GetDistinctAssetTypesAsync(cancellationToken);
        return Ok(assetTypes);
    }
}