using Application.Contractors;
using Application.Framework;
using Application.RequestDTO;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentMethodController : ControllerBase
{
    private readonly IPaymentMethodService _paymentMethodService;

    public PaymentMethodController(IPaymentMethodService paymentMethodService)
    {
        _paymentMethodService = paymentMethodService;
    }

    [HttpGet]
    public async Task<IEnumerable<PaymentMethodListResponse>> GetPaymentMethods(CancellationToken cancellationToken)
    {
        return await _paymentMethodService.ListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("active")]
    public async Task<IEnumerable<PaymentMethodListResponse>> GetActivePaymentMethods(CancellationToken cancellationToken)
    {
        return await _paymentMethodService.GetActiveListAsync(cancellationToken);
    }

    [HttpGet]
    [Route("category/{category}")]
    public async Task<IEnumerable<PaymentMethodListResponse>> GetByCategory(string category, CancellationToken cancellationToken)
    {
        return await _paymentMethodService.GetByCategoryAsync(category, cancellationToken);
    }

    [HttpGet]
    [Route("Lookup")]
    public async Task<IEnumerable<Lookup<int>>> GetLookup(CancellationToken cancellationToken)
    {
        Expression<Func<PaymentMethod, bool>> predicate = x => x.IsActive;
        return await _paymentMethodService.GetLookup(predicate, cancellationToken);
    }

    [HttpGet]
    [Route("get-with-pagination")]
    public async Task<PaginationResult<PaymentMethodListResponse>> GetWithPagination([FromQuery] PaginationQuery requestQuery, CancellationToken cancellationToken)
    {
        return await _paymentMethodService.PaginationListAsync(requestQuery, cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PaymentMethodResponse>> GetPaymentMethod(int id, CancellationToken cancellationToken)
    {
        var paymentMethod = await _paymentMethodService.GetByIdAsync(id, cancellationToken);
        if (paymentMethod == null)
        {
            return NotFound();
        }
        return Ok(paymentMethod);
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult<PaymentMethodResponse>> GetPaymentMethodByCode(string code, CancellationToken cancellationToken)
    {
        var paymentMethod = await _paymentMethodService.GetByCodeAsync(code, cancellationToken);
        if (paymentMethod == null)
        {
            return NotFound();
        }
        return Ok(paymentMethod);
    }

    [HttpGet("generate-code")]
    public async ValueTask<CodeResponse> GenerateCode(CancellationToken cancellationToken)
    {
        var response = await _paymentMethodService.GenerateCode(cancellationToken);
        return new CodeResponse(response);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentMethodResponse>> CreatePaymentMethod(PaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var paymentMethod = await _paymentMethodService.AddAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPaymentMethod), new { id = paymentMethod.Id }, paymentMethod);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PaymentMethodResponse>> UpdatePaymentMethod(int id, PaymentMethodRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updatedPaymentMethod = await _paymentMethodService.UpdateAsync(id, request, cancellationToken);
            return Ok(updatedPaymentMethod);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePaymentMethod(int id, CancellationToken cancellationToken)
    {
        var result = await _paymentMethodService.DeleteAsync(id, cancellationToken);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpDelete("batch")]
    public async Task<IActionResult> DeletePaymentMethods([FromBody] List<int> ids, CancellationToken cancellationToken)
    {
        var result = await _paymentMethodService.DeleteBatchAsync(ids, cancellationToken);
        if (!result)
        {
            return BadRequest();
        }
        return NoContent();
    }
}