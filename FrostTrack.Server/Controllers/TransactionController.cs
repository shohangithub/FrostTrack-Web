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
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? transactionType,
        [FromQuery] string? transactionFlow,
        CancellationToken cancellationToken)
    {
        var result = await _service.ListAsync(cancellationToken);

        // Apply filters if provided
        if (startDate.HasValue)
        {
            result = result.Where(t => t.TransactionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            result = result.Where(t => t.TransactionDate <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(transactionType))
        {
            result = result.Where(t => t.TransactionType == transactionType);
        }

        if (!string.IsNullOrEmpty(transactionFlow))
        {
            result = result.Where(t => t.TransactionFlow == transactionFlow);
        }

        return Ok(result);
    }

    [HttpGet("pagination")]
    public async Task<IActionResult> GetPagination([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        var result = await _service.PaginationListAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        if (result == null)
            return NotFound();
        return result;
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> Create([FromBody] TransactionRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.AddAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TransactionResponse>> Update(Guid id, [FromBody] TransactionRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        return result;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await _service.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<Guid> ids, CancellationToken cancellationToken)
    {
        return await _service.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("generate-code")]
    public async Task<ActionResult<CodeResponse>> GenerateCode(CancellationToken cancellationToken)
    {
        var code = await _service.GenerateTransactionCode(cancellationToken);
        return new CodeResponse(code);
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
