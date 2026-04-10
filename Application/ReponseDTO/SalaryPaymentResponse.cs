namespace Application.ReponseDTO;

public record SalaryPaymentResponse(
    int Id,
    int EmployeeId,
    string EmployeeName,
    string EmployeeCode,
    int Month,
    int Year,
    decimal BasicSalary,
    decimal Bonus,
    decimal Deduction,
    decimal NetAmount,
    DateTime PaymentDate,
    string PaymentMethod,
    string? Note,
    string TransactionId,
    string TransactionCode,
    DateTime CreatedAt
);

public record SalaryPaymentListResponse(
    Guid Id,
    string EmployeeName,
    string EmployeeCode,
    string Period,
    decimal BasicSalary,
    decimal NetAmount,
    DateTime PaymentDate,
    string PaymentMethod,
    DateTime CreatedAt
);

public record EmployeeForSalaryResponse(
    int Id,
    string Name,
    string Code,
    string Designation,
    decimal Salary,
    DateTime? LastPaymentDate,
    string? LastPaymentPeriod
);

public class MonthlyPaymentSummaryResponse
{
    public int Month { get; set; }
    public int Year { get; set; }
    public int TotalEmployees { get; set; }
    public decimal TotalBasicSalary { get; set; }
    public decimal TotalBonus { get; set; }
    public decimal TotalDeduction { get; set; }
    public decimal TotalNetAmount { get; set; }
    public List<SalaryPaymentListResponse> Payments { get; set; } = new();
}
