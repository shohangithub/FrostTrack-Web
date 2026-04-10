using FluentValidation;
using Application.RequestDTO;
using Domain.Entitites;
using Application.Contractors;
using Microsoft.EntityFrameworkCore;

namespace Application.Validators;

public class SalaryPaymentValidator : AbstractValidator<SalaryPaymentRequest>
{
    private readonly IRepository<Employee, int> _employeeRepository;
    private readonly IRepository<SalaryPayment, int> _salaryPaymentRepository;

    public SalaryPaymentValidator(
        IRepository<Employee, int> employeeRepository,
        IRepository<SalaryPayment, int> salaryPaymentRepository)
    {
        _employeeRepository = employeeRepository;
        _salaryPaymentRepository = salaryPaymentRepository;

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
        var exists = await _salaryPaymentRepository.Query()
            .Include(x => x.Transaction)
            .AnyAsync(x => x.EmployeeId == request.EmployeeId &&
                           x.Month == request.Month &&
                           x.Year == request.Year &&
                           !x.Transaction!.IsDeleted, cancellationToken);
        return !exists;
    }
}

