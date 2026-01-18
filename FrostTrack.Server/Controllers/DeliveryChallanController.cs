using Application.Common;
using Application.Contractors;
using Application.RequestDTO;
using Application.ReponseDTO;
using Application.Framework;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveryChallanController : ControllerBase
{
    private readonly IDeliveryChallanService _service;

    public DeliveryChallanController(IDeliveryChallanService service)
    {
        _service = service;
    }

    [HttpGet("list")]
    public async Task<ActionResult<IEnumerable<DeliveryChallanListResponse>>> GetList(CancellationToken cancellationToken)
    {
        var result = await _service.ListAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("get-with-pagination")]
    public async Task<ActionResult<PaginationResult<DeliveryChallanListResponse>>> GetWithPagination(
        [FromQuery] PaginationQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _service.PaginationListAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DeliveryChallanResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<DeliveryChallanResponse>> Create(
        [FromBody] DeliveryChallanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.AddAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DeliveryChallanResponse>> Update(
        Guid id,
        [FromBody] DeliveryChallanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.DeleteAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("delete-batch")]
    public async Task<ActionResult<bool>> DeleteBatch(
        [FromBody] List<Guid> ids,
        CancellationToken cancellationToken)
    {
        var result = await _service.DeleteBatchAsync(ids, cancellationToken);
        return Ok(result);
    }

    [HttpGet("generate-challan-number")]
    public async Task<ActionResult<string>> GenerateChallanNumber(CancellationToken cancellationToken)
    {
        var result = await _service.GenerateChallanNumber(cancellationToken);
        return Ok(new { code = result });
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<DeliveryChallanResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.UpdateStatusAsync(id, request.Status, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}

public record UpdateStatusRequest(string Status);
