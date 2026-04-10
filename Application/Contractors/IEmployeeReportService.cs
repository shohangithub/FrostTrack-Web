namespace Application.Contractors;

public interface IEmployeeReportService
{
    Task<IEnumerable<EmployeeReportResponse>> GetEmployeeReportAsync(
        string? department = null,
        string? designation = null,
        string? employmentType = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);
}
