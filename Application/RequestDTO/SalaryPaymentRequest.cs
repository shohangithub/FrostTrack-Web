namespace Application.RequestDTO;

public record SalaryPaymentRequest(
    int EmployeeId,
    int Month,
    int Year,
    decimal BasicSalary,
    decimal Bonus,
    decimal Deduction,
    string PaymentMethod,
    string? Note
);
