namespace Application.ReponseDTO;

public record EmployeeReportResponse(
    int Id,
    string EmployeeCode,
    string EmployeeName,
    string? Department,
    string? Designation,
    string? EmploymentType,
    string? Email,
    string? Phone,
    string? Address,
    DateTime? DateOfBirth,
    DateTime? JoiningDate,
    decimal Salary,
    string? BloodGroup,
    string? NationalId,
    string? EmergencyContact,
    string? BankAccount,
    string Status
);
