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
public class ProductDeliveryController : ControllerBase
{
    private readonly IProductDeliveryService _service;

    public ProductDeliveryController(IProductDeliveryService service)
    {
        _service = service;
    }

    [HttpGet("get-with-pagination")]
    public async Task<ActionResult<PaginationResult<ProductDeliveryListResponse>>> GetWithPagination([FromQuery] PaginationQuery query)
    {
        var result = await _service.GetWithPaginationAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDeliveryResponse>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDeliveryResponse>> Create([FromBody] ProductDeliveryRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDeliveryResponse>> Update(int id, [FromBody] ProductDeliveryRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        return Ok(result);
    }

    [HttpPost("DeleteBatch")]
    public async Task<ActionResult<bool>> BatchDelete([FromBody] int[] ids)
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
