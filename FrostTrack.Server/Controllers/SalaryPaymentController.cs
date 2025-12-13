using Application.Contractors;
using Application.RequestDTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SalaryPaymentController : ControllerBase
{
    private readonly ISalaryPaymentService _salaryPaymentService;

    public SalaryPaymentController(ISalaryPaymentService salaryPaymentService)
    {
        _salaryPaymentService = salaryPaymentService;
    }

    /// <summary>
    /// Get all employees with salary information for payment
    /// </summary>
    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployeesForPayment(CancellationToken cancellationToken)
    {
        var result = await _salaryPaymentService.GetEmployeesForPaymentAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new salary payment
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSalaryPayment([FromBody] SalaryPaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await _salaryPaymentService.CreateSalaryPaymentAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get salary payment list with pagination
    /// </summary>
    [HttpGet("get-with-pagination")]
    public async Task<ActionResult<PaginationResult<SalaryPaymentListResponse>>> GetWithPagination(
        [FromQuery] PaginationQuery requestQuery,
        [FromQuery] int? employeeId,
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var result = await _salaryPaymentService.PaginationListAsync(requestQuery, employeeId, month, year, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get salary payment list
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetSalaryPaymentList(CancellationToken cancellationToken)
    {
        var result = await _salaryPaymentService.GetSalaryPaymentListAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get salary payment history with optional filters
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetPaymentHistory(
        [FromQuery] int? employeeId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var result = await _salaryPaymentService.GetPaymentHistoryAsync(employeeId, startDate, endDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get monthly salary payment report
    /// </summary>
    [HttpGet("monthly-report")]
    public async Task<IActionResult> GetMonthlyReport(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var result = await _salaryPaymentService.GetMonthlyPaymentReportAsync(month, year, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get salary payment by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _salaryPaymentService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete salary payment (only allowed within 24 hours)
    /// </summary>
    [HttpDelete("{transactionId}")]
    public async Task<IActionResult> DeleteSalaryPayment(Guid transactionId, CancellationToken cancellationToken)
    {
        var result = await _salaryPaymentService.DeleteSalaryPaymentAsync(transactionId, cancellationToken);
        return Ok(result);
    }
}
