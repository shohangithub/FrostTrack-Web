namespace Application.Services;

public class EmployeeReportService : IEmployeeReportService
{
    private readonly IRepository<Employee, int> _employeeRepository;
    private readonly Guid _tenantId;

    public EmployeeReportService(
        IRepository<Employee, int> employeeRepository,
        ITenantProvider tenantProvider)
    {
        _employeeRepository = employeeRepository;
        _tenantId = tenantProvider.GetTenantId();
    }

    public async Task<IEnumerable<EmployeeReportResponse>> GetEmployeeReportAsync(
        string? department = null,
        string? designation = null,
        string? employmentType = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = _employeeRepository.Query()
            .Where(e => e.TenantId == _tenantId);

        if (!string.IsNullOrWhiteSpace(department))
            query = query.Where(e => e.Department == department);

        if (!string.IsNullOrWhiteSpace(designation))
            query = query.Where(e => e.Designation == designation);

        if (!string.IsNullOrWhiteSpace(employmentType))
            query = query.Where(e => e.EmploymentType == employmentType);

        if (isActive.HasValue)
            query = query.Where(e => e.IsActive == isActive.Value);

        var employees = await query
            .OrderBy(e => e.Department)
            .ThenBy(e => e.EmployeeName)
            .ToListAsync(cancellationToken);

        return employees.Select(e => new EmployeeReportResponse(
            e.Id,
            e.EmployeeCode,
            e.EmployeeName,
            e.Department,
            e.Designation,
            e.EmploymentType,
            e.Email,
            e.Phone,
            e.Address,
            e.DateOfBirth,
            e.JoiningDate,
            e.Salary,
            e.BloodGroup,
            e.NationalId,
            e.EmergencyContact,
            e.BankAccount,
            e.Status
        ));
    }
}
