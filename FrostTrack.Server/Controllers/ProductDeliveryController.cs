using Application.Contractors;
using Application.Framework;
using Application.ReponseDTO;
using Application.RequestDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DeliveryController : ControllerBase
{
    private readonly IDeliveryService _service;

    public DeliveryController(IDeliveryService service)
    {
        _service = service;
    }

    [HttpGet("get-with-pagination")]
    public async Task<ActionResult<PaginationResult<DeliveryListResponse>>> GetWithPagination([FromQuery] PaginationQuery query)
    {
        var result = await _service.GetWithPaginationAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DeliveryResponse>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<DeliveryResponse>> Create([FromBody] DeliveryRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DeliveryResponse>> Update(Guid id, [FromBody] DeliveryRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return Ok(result);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> BatchDelete([FromBody] Guid[] ids)
    {
        var result = await _service.BatchDeleteAsync(ids);
        return Ok(result);
    }

    [HttpGet("generate-delivery-number")]
    public async Task<ActionResult<CodeResponse>> GenerateDeliveryNumber()
    {
        var number = await _service.GenerateDeliveryNumberAsync();
        return Ok(new CodeResponse { Code = number });
    }

    [HttpGet("customer-stock/{customerId}")]
    public async Task<ActionResult<List<CustomerStockResponse>>> GetCustomerStock(int customerId)
    {
        var result = await _service.GetCustomerStockAsync(customerId);
        return Ok(result);
    }
}
