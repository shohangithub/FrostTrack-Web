using Microsoft.AspNetCore.Authorization;

namespace FrostTrack.Server.Controllers;

/// <summary>Balance Sheet setup: start date / fiscal year, opening balances and year-end adjustments.</summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class BalanceSheetEntryController : ControllerBase
{
    private readonly IBalanceSheetEntryService _service;

    public BalanceSheetEntryController(IBalanceSheetEntryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IEnumerable<BalanceSheetEntryResponse>> GetAll(CancellationToken cancellationToken)
    {
        return await _service.ListAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BalanceSheetEntryResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _service.GetByIdAsync(id, cancellationToken);
        return entry is null ? NotFound() : entry;
    }

    [HttpPost]
    public async Task<ActionResult<BalanceSheetEntryResponse?>> Post(BalanceSheetEntryRequest request, CancellationToken cancellationToken)
    {
        return await _service.AddAsync(request, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BalanceSheetEntryResponse?>> Put(Guid id, BalanceSheetEntryRequest request, CancellationToken cancellationToken)
    {
        return await _service.UpdateAsync(id, request, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async ValueTask<bool> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await _service.DeleteAsync(id, cancellationToken);
    }

    [HttpGet("settings")]
    public async Task<BalanceSheetSettingsResponse> GetSettings(CancellationToken cancellationToken)
    {
        return await _service.GetSettingsAsync(cancellationToken);
    }

    [HttpPut("settings")]
    public async Task<BalanceSheetSettingsResponse> UpdateSettings(BalanceSheetSettingsRequest request, CancellationToken cancellationToken)
    {
        return await _service.UpdateSettingsAsync(request, cancellationToken);
    }
}
