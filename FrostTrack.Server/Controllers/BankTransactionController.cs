namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BankTransactionController : ControllerBase
{
    private readonly IBankTransactionService _bankTransactionService;

    public BankTransactionController(IBankTransactionService bankTransactionService)
    {
        _bankTransactionService = bankTransactionService;
    }

    [HttpGet]
    public async Task<IEnumerable<BankTransactionListResponse>> GetBankTransactions(CancellationToken cancellationToken)
    {
        return await _bankTransactionService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("lookup")]
    public async Task<IEnumerable<Lookup<long>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<BankTransaction, bool>> predicate = x => true;
        return await _bankTransactionService.GetLookup(predicate, cancellationToken);
    }

    [HttpPost]
    [Route("getall")]
    public async Task<PaginationResult<BankTransactionListResponse>> GetAll([FromBody] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _bankTransactionService.PaginationListAsync(requestQuery, cancellationToken);
    }
    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<BankTransactionListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _bankTransactionService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BankTransactionResponse>> GetBankTransaction(long id, CancellationToken cancellationToken)
    {
        var bankTransaction = await _bankTransactionService.GetByIdAsync(id, cancellationToken);
        if (bankTransaction == null)
        {
            return NotFound();
        }
        return bankTransaction;
    }

    [HttpPost]
    public async Task<ActionResult<BankTransactionResponse>> CreateBankTransaction([FromBody] BankTransactionRequest bankTransaction, CancellationToken cancellationToken)
    {
        var result = await _bankTransactionService.AddAsync(bankTransaction, cancellationToken);
        return CreatedAtAction(nameof(GetBankTransaction), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BankTransactionResponse>> UpdateBankTransaction(int id, [FromBody] BankTransactionRequest bankTransaction, CancellationToken cancellationToken)
    {
        var result = await _bankTransactionService.UpdateAsync(id, bankTransaction, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBankTransaction(int id, CancellationToken cancellationToken)
    {
        await _bankTransactionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost]
    [Route("deletemultiple")]
    public async Task<IActionResult> DeleteMultiple([FromBody] List<long> ids, CancellationToken cancellationToken)
    {
        await _bankTransactionService.DeleteBatchAsync(ids, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [Route("generate-code")]
    public async Task<ActionResult<CodeResponse>> GenerateCode(CancellationToken cancellationToken)
    {
        var code = await _bankTransactionService.GenerateCode(cancellationToken);
        return Ok(new CodeResponse(code));
    }
}