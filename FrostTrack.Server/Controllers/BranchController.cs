using Microsoft.AspNetCore.Authorization;

namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]

//[Authorize(Roles = $"{RoleNames.SuperAdmin}, {RoleNames.Admin}")]
public class BranchController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<IEnumerable<BranchListResponse>> GetBranchCategories(CancellationToken cancellationToken)
    {
        return await _branchService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<int>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<Branch, bool>> predicate = x => x.IsActive == x.IsActive;
        return await _branchService.GetLookup(predicate, cancellationToken);
    }


    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<BranchListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _branchService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BranchResponse>> GetBranchCategories(short id, CancellationToken cancellationToken)
    {

        var branch = await _branchService.GetByIdAsync(id, cancellationToken);
        if (branch == null)
        {
            return NotFound();
        }

        return branch;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BranchResponse>> PutBranch(short id, BranchRequest branch)
    {
        var response = await _branchService.UpdateAsync(id, branch);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<BranchResponse>> PostBranch(BranchRequest branch, CancellationToken cancellationToken)
    {
        return await _branchService.AddAsync(branch, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteBranch(short id, CancellationToken cancellationToken)
    {
        return await _branchService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<int> ids, CancellationToken cancellationToken)
    {
        return await _branchService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsBranchExists")]
    public async ValueTask<bool> IsBranchExists([FromQuery] short id, CancellationToken cancellationToken)
    {
        var response = await _branchService.IsExistsAsync(id, cancellationToken);
        return response;
    }

    [HttpGet("generate-code")]
    public async ValueTask<CodeResponse> GenerateCode(CancellationToken cancellationToken)
    {
        var response = await _branchService.GenerateCode(cancellationToken);
        return new CodeResponse(response);
    }
}
