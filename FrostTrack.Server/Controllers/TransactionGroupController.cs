namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TransactionGroupController : ControllerBase
{
    private readonly ITransactionGroupService _transactionGroupService;

    public TransactionGroupController(ITransactionGroupService transactionGroupService)
    {
        _transactionGroupService = transactionGroupService;
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<Guid>>> GetLookup(CancellationToken cancellationToken)
    {
        return await _transactionGroupService.GetLookup(cancellationToken);
    }

    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<TransactionGroupListResponse>> GetWithPagination(
        [FromQuery] PaginationQuery requestQuery,
        CancellationToken cancellationToken)
    {
        return await _transactionGroupService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionGroupResponse>> GetTransactionGroup(Guid id, CancellationToken cancellationToken)
    {
        var group = await _transactionGroupService.GetByIdAsync(id, cancellationToken);
        if (group == null)
        {
            return NotFound();
        }

        return group;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TransactionGroupResponse?>> PutTransactionGroup(
        Guid id,
        TransactionGroupRequest request,
        CancellationToken cancellationToken)
    {
        return await _transactionGroupService.UpdateAsync(id, request, cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionGroupResponse?>> PostTransactionGroup(
        TransactionGroupRequest request,
        CancellationToken cancellationToken)
    {
        return await _transactionGroupService.AddAsync(request, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteTransactionGroup(Guid id, CancellationToken cancellationToken)
    {
        return await _transactionGroupService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<Guid> ids, CancellationToken cancellationToken)
    {
        return await _transactionGroupService.DeleteBatchAsync(ids, cancellationToken);
    }
}
