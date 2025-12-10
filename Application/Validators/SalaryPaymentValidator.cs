using FluentValidation;
using Application.RequestDTO;
using Domain.Entitites;
using Application.Contractors;
using Microsoft.EntityFrameworkCore;

namespace Application.Validators;

public class SalaryPaymentValidator : AbstractValidator<SalaryPaymentRequest>
{
    private readonly IRepository<Employee, int> _employeeRepository;
    private readonly IRepository<Transaction, Guid> _transactionRepository;

    public SalaryPaymentValidator(
        IRepository<Employee, int> employeeRepository,
        IRepository<Transaction, Guid> transactionRepository,
        string transactionCode)
    {
        _employeeRepository = employeeRepository;
        _transactionRepository = transactionRepository;


        RuleFor(x => transactionCode)
            .NotEmpty().WithMessage("Transaction code is required")
            .MaximumLength(50).WithMessage("Transaction code cannot exceed 50 characters")
            .MustAsync(BeUniqueTransactionCode).WithMessage("Transaction code already exists");


        RuleFor(x => x.EmployeeId)
            .GreaterThan(0).WithMessage("Employee is required")
            .MustAsync(BeValidEmployee).WithMessage("Employee not found or inactive");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("Month must be between 1 and 12");

        RuleFor(x => x.Year)
            .GreaterThan(2000).WithMessage("Year must be greater than 2000")
            .LessThanOrEqualTo(DateTime.UtcNow.Year).WithMessage("Year cannot be in the future");

        RuleFor(x => x.BasicSalary)
            .GreaterThan(0).WithMessage("Basic salary must be greater than 0");

        RuleFor(x => x.Bonus)
            .GreaterThanOrEqualTo(0).WithMessage("Bonus cannot be negative");

        RuleFor(x => x.Deduction)
            .GreaterThanOrEqualTo(0).WithMessage("Deduction cannot be negative");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required")
            .Must(BeValidPaymentMethod).WithMessage("Invalid payment method");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Note));

        RuleFor(x => x)
            .MustAsync(NotHaveDuplicatePayment).WithMessage("Salary payment for this employee and period already exists");
    }

    private async Task<bool> BeValidEmployee(int employeeId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        return employee != null && employee.IsActive;
    }


    private async Task<bool> BeUniqueTransactionCode(string transactionCode, CancellationToken cancellationToken)
    {
        var exists = await _transactionRepository.Query()
            .AnyAsync(x => x.TransactionCode == transactionCode, cancellationToken);
        return !exists;
    }

    private bool BeValidPaymentMethod(string paymentMethod)
    {
        return paymentMethod == PaymentMethods.CASH ||
               paymentMethod == PaymentMethods.BANK_TRANSFER ||
               paymentMethod == PaymentMethods.CHEQUE ||
               paymentMethod == PaymentMethods.CARD ||
               paymentMethod == PaymentMethods.MOBILE_BANKING ||
               paymentMethod == PaymentMethods.CREDIT;
    }

    private async Task<bool> NotHaveDuplicatePayment(SalaryPaymentRequest request, CancellationToken cancellationToken)
    {
        var period = $"{request.Month:D2}/{request.Year}";
        var exists = await _transactionRepository.Query()
            .AnyAsync(x => x.TransactionType == TransactionTypes.SALARY &&
                          x.EntityId == request.EmployeeId.ToString() &&
                          x.Description.Contains(period) &&
                          !x.IsDeleted, cancellationToken);
        return !exists;
    }
}
