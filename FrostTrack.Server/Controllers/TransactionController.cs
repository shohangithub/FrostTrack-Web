using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _service;

    public TransactionController(ITransactionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _service.ListAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("pagination")]
    public async Task<IActionResult> GetPagination([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        var result = await _service.PaginationListAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransactionRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.AddAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TransactionRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("batch")]
    public async Task<IActionResult> DeleteBatch([FromBody] List<Guid> ids, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteBatchAsync(ids, cancellationToken);
        return Ok(result);
    }

    [HttpGet("generate-code")]
    public async Task<IActionResult> GenerateCode(CancellationToken cancellationToken)
    {
        var result = await _service.GenerateTransactionCode(cancellationToken);
        return Ok(result);
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> GetLookup(CancellationToken cancellationToken)
    {
        var result = await _service.GetLookup(x => true, cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetSummaryAsync(startDate, endDate, branchId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("cash-flow")]
    public async Task<IActionResult> GetCashFlow(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? branchId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetCashFlowAsync(startDate, endDate, branchId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/soft-delete")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.SoftDeleteAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.RestoreAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.ArchiveAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/unarchive")]
    public async Task<IActionResult> Unarchive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.UnarchiveAsync(id, cancellationToken);
        return Ok(result);
    }
}
