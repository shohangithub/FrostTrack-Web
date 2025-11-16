namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Permission(ERoles.Admin)]
public class BankController : ControllerBase
{
    private readonly IBankService _bankService;

    public BankController(IBankService bankService)
    {
        _bankService = bankService;
    }

    [HttpGet]
    [Route("get-list")]
    public async Task<IEnumerable<BankListResponse>> GetBanks(CancellationToken cancellationToken)
    {
        return await _bankService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<int>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<Bank, bool>> predicate = x => x.IsActive == true;
        return await _bankService.GetLookup(predicate, cancellationToken);
    }

    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<BankListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _bankService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BankResponse>> GetBank(int id, CancellationToken cancellationToken)
    {
        var bank = await _bankService.GetByIdAsync(id, cancellationToken);
        if (bank == null)
        {
            return NotFound();
        }

        return bank;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BankResponse>> PutBank(int id, BankRequest bank)
    {
        var response = await _bankService.UpdateAsync(id, bank);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<BankResponse>> PostBank(BankRequest bank, CancellationToken cancellationToken)
    {
        return await _bankService.AddAsync(bank, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteBank(int id, CancellationToken cancellationToken)
    {
        return await _bankService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<int> ids, CancellationToken cancellationToken)
    {
        return await _bankService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsBankExists")]
    public async ValueTask<bool> IsBankExists([FromQuery] int id, CancellationToken cancellationToken)
    {
        var response = await _bankService.IsExistsAsync(id, cancellationToken);
        return response;
    }

    [HttpGet("generate-code")]
    public async ValueTask<CodeResponse> GenerateCode(CancellationToken cancellationToken)
    {
        var response = await _bankService.GenerateCode(cancellationToken);
        return new CodeResponse(response);
    }

    [HttpGet("{id}/balance")]
    public async Task<ActionResult<decimal>> GetBankBalance(int id, CancellationToken cancellationToken)
    {
        var balance = await _bankService.GetCurrentBalanceAsync(id, cancellationToken);
        return balance;
    }
}