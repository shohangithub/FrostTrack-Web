namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
//[Permission(ERoles.Admin)]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }


    [HttpGet]
    [Route("get-list")]
    public async Task<IEnumerable<CustomerListResponse>> GetCustomers(CancellationToken cancellationToken)
    {
        return await _customerService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<int>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<Customer, bool>> predicate = x => x.IsActive == x.IsActive;
        return await _customerService.GetLookup(predicate, cancellationToken);
    }


    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<CustomerListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _customerService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerResponse>> GetProductCategories(short id, CancellationToken cancellationToken)
    {

        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer == null)
        {
            return NotFound();
        }

        return customer;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerResponse>> PutCustomer(short id, CustomerRequest customer)
    {
        var response = await _customerService.UpdateAsync(id, customer);
        return response;
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> PostCustomer(CustomerRequest customer, CancellationToken cancellationToken)
    {
        return await _customerService.AddAsync(customer, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async ValueTask<bool> DeleteCustomer(short id, CancellationToken cancellationToken)
    {
        return await _customerService.DeleteAsync(id, cancellationToken);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> DeleteBatch([FromBody] List<int> ids, CancellationToken cancellationToken)
    {
        return await _customerService.DeleteBatchAsync(ids, cancellationToken);
    }

    [HttpGet("IsCustomerExists")]
    public async ValueTask<bool> IsCustomerExists([FromQuery] short id, CancellationToken cancellationToken)
    {
        var response = await _customerService.IsExistsAsync(id, cancellationToken);
        return response;
    }

    [HttpGet("generate-code")]
    public async ValueTask<CodeResponse> GenerateCode(CancellationToken cancellationToken)
    {
        var response = await _customerService.GenerateCode(cancellationToken);
        return new CodeResponse(response);
    }
}
