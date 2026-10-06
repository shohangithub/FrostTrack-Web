using Microsoft.AspNetCore.Authorization;

namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly IEditWindowGuard _editWindowGuard;

    public CompanyController(ICompanyService companyService, IEditWindowGuard editWindowGuard)
    {
        _companyService = companyService;
        _editWindowGuard = editWindowGuard;
    }

    [Authorize]
    [HttpGet("edit-window-policy")]
    public async Task<EditWindowPolicyResponse> GetEditWindowPolicy(CancellationToken cancellationToken)
    {
        return await _editWindowGuard.GetPolicyAsync(cancellationToken);
    }

    [HttpGet]
    public async Task<IEnumerable<CompanyListResponse>> GetCompanies(CancellationToken cancellationToken)
    {
        return await _companyService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<CompanyListResponse>> GetWithPagination(
        [FromQuery] PaginationQuery requestQuery,
        CancellationToken cancellationToken)
    {
        return await _companyService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CompanyResponse>> GetCompany(int id, CancellationToken cancellationToken)
    {
        var company = await _companyService.GetByIdAsync(id, cancellationToken);
        if (company == null)
        {
            return NotFound();
        }

        return company;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CompanyResponse>> PutCompany(int id, CompanyRequest request)
    {
        try
        {
            var response = await _companyService.UpdateAsync(id, request);
            return response;
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> PostCompany(CompanyRequest request, CancellationToken cancellationToken)
    {
        var response = await _companyService.AddAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCompany), new { id = response.Id }, response);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> DeleteCompany(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await _companyService.DeleteAsync(id, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
