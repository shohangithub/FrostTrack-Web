using Application.Contractors;
using Microsoft.AspNetCore.Mvc;

namespace FrostTrack.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeeReportController : ControllerBase
{
    private readonly IEmployeeReportService _employeeReportService;

    public EmployeeReportController(IEmployeeReportService employeeReportService)
    {
        _employeeReportService = employeeReportService;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployeeReport(
        [FromQuery] string? department,
        [FromQuery] string? designation,
        [FromQuery] string? employmentType,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await _employeeReportService.GetEmployeeReportAsync(
            department,
            designation,
            employmentType,
            isActive,
            cancellationToken);

        return Ok(result);
    }
}
