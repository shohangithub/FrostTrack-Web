namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TransactionHeadController : ControllerBase
{
    private readonly ITransactionHeadService _transactionHeadService;

    public TransactionHeadController(ITransactionHeadService transactionHeadService)
    {
        _transactionHeadService = transactionHeadService;
    }

    [HttpGet]
    public async Task<IEnumerable<TransactionHeadListResponse>> GetTransactionHeads(CancellationToken cancellationToken)
    {
        return await _transactionHeadService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<Guid>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<TransactionHead, bool>> predicate = x => x.IsActive == x.IsActive;
        return await _transactionHeadService.GetLookup(predicate, cancellationToken);
    }

    [HttpGet]
    [Route("TransactionLookup")]
    public async Task<IEnumerable<TransactionHeadLookup>> GetTransactionLookup(CancellationToken cancellationToken)
    {
        return await _transactionHeadService.GetTransactionLookup(cancellationToken);
    }

    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<TransactionHeadListResponse>> GetWithPagination(
        [FromQuery] PaginationQuery requestQuery,
        CancellationToken cancellationToken)
    {
        return await _transactionHeadService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionHeadResponse>> GetTransactionHead(Guid id, CancellationToken cancellationToken)
    {
        var transactionHead = await _transactionHeadService.GetByIdAsync(id, cancellationToken);
        if (transactionHead == null)
        {
            return NotFound();
        }

        return transactionHead;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TransactionHeadResponse>> PutTransactionHead(Guid id, TransactionHeadRequest transactionHead)
    {
        var response = await _transactionHeadService.UpdateAsync(id, transactionHead);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<TransactionHeadResponse>> PostTransactionHead(
        TransactionHeadRequest transactionHead,
        CancellationToken cancellationToken)
    {
        return await _transactionHeadService.AddAsync(transactionHead, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteTransactionHead(Guid id, CancellationToken cancellationToken)
    {
        return await _transactionHeadService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<Guid> ids, CancellationToken cancellationToken)
    {
        return await _transactionHeadService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsTransactionHeadExists")]
    public async ValueTask<bool> IsTransactionHeadExists([FromQuery] Guid id, CancellationToken cancellationToken)
    {
        var response = await _transactionHeadService.IsExistsAsync(id, cancellationToken);
        return response;
    }
}
