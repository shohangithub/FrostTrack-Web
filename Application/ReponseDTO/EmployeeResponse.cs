namespace Application.ReponseDTO;

public record EmployeeResponse(
    int Id,
    string EmployeeName,
    string EmployeeCode,
    string? EmploymentType,
    string? Email,
    string? Phone,
    string? Address,
    DateTime? DateOfBirth,
    DateTime? JoiningDate,
    string? Department,
    string? Designation,
    decimal Salary,
    string? EmergencyContact,
    string? BloodGroup,
    string? NationalId,
    string? PassportNumber,
    string? BankAccount,
    string? Notes,
    string? PhotoUrl,
    bool IsActive,
    string Status
    );

public record EmployeeListResponse(
    int Id,
    string EmployeeName,
    string EmployeeCode,
    string? EmploymentType,
    string? Email,
    string? Phone,
    string? Address,
    DateTime? DateOfBirth,
    DateTime? JoiningDate,
    string? Department,
    string? Designation,
    decimal Salary,
    string? EmergencyContact,
    string? BloodGroup,
    string? NationalId,
    string? PassportNumber,
    string? BankAccount,
    string? Notes,
    string? PhotoUrl,
    string Status
    );