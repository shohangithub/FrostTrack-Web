namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Permission(ERoles.Admin)]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    [Route("get-list")]
    public async Task<IEnumerable<EmployeeListResponse>> GetEmployees(CancellationToken cancellationToken)
    {
        return await _employeeService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<int>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<Employee, bool>> predicate = x => x.IsActive == true;
        return await _employeeService.GetLookup(predicate, cancellationToken);
    }

    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<EmployeeListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _employeeService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeResponse>> GetEmployee(int id, CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetByIdAsync(id, cancellationToken);
        if (employee == null)
        {
            return NotFound();
        }

        return employee;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EmployeeResponse>> PutEmployee(int id, EmployeeRequest employee)
    {
        var response = await _employeeService.UpdateAsync(id, employee);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeResponse>> PostEmployee(EmployeeRequest employee, CancellationToken cancellationToken)
    {
        return await _employeeService.AddAsync(employee, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteEmployee(int id, CancellationToken cancellationToken)
    {
        return await _employeeService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<int> ids, CancellationToken cancellationToken)
    {
        return await _employeeService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsEmployeeExists")]
    public async ValueTask<bool> IsEmployeeExists([FromQuery] int id, CancellationToken cancellationToken)
    {
        var response = await _employeeService.IsExistsAsync(id, cancellationToken);
        return response;
    }

    [HttpGet("generate-code")]
    public async Task<ActionResult<CodeResponse>> GenerateCode(CancellationToken cancellationToken)
    {
        var code = await _employeeService.GenerateCode(cancellationToken);
        return new CodeResponse(code);
    }

    [HttpGet("departments")]
    public async Task<ActionResult<IEnumerable<string>>> GetDistinctDepartments(CancellationToken cancellationToken)
    {
        var departments = await _employeeService.GetDistinctDepartmentsAsync(cancellationToken);
        return Ok(departments);
    }

    [HttpGet("designations")]
    public async Task<ActionResult<IEnumerable<string>>> GetDistinctDesignations(CancellationToken cancellationToken)
    {
        var designations = await _employeeService.GetDistinctDesignationsAsync(cancellationToken);
        return Ok(designations);
    }
}