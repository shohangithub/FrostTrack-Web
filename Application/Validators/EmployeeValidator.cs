using Application.RequestDTO;

namespace Application.Validators;

internal class EmployeeValidator : AbstractValidator<EmployeeRequest>
{
    public EmployeeValidator(IRepository<Employee, int> repository, int id = 0)
    {
        RuleFor(x => x.EmployeeName)
            .NotEmpty()
            .WithMessage("Employee name is required")
            .MaximumLength(200)
            .WithMessage("Employee name cannot exceed 200 characters");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format");

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone cannot exceed 20 characters");

        RuleFor(x => x.Salary)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Salary must be greater than or equal to 0");

        RuleFor(x => x.NationalId)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.NationalId))
            .WithMessage("National ID cannot exceed 50 characters");

        RuleFor(x => x.PassportNumber)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.PassportNumber))
            .WithMessage("Passport number cannot exceed 50 characters");

        if (id != 0)
        {
            RuleFor(cmd => cmd).MustAsync(async (employee, cancellation) =>
            {
                return await repository.Query().AnyAsync(q => q.Id == id);
            }).WithMessage("Employee not found with this id");

            RuleFor(cmd => cmd.EmployeeCode).NotNull().MinimumLength(1).MustAsync(async (code, cancellation) =>
            {
                return !await repository.Query().AnyAsync(q => q.EmployeeCode.ToLower() == code.ToLower() && q.Id != id);
            }).WithMessage("Employee Code must be unique");
        }
        else
        {
            RuleFor(cmd => cmd.EmployeeCode).NotNull().MinimumLength(1).MustAsync(async (code, cancellation) =>
            {
                return !await repository.Query().AnyAsync(q => q.EmployeeCode.ToLower() == code.ToLower());
            }).WithMessage("Employee Code must be unique");
        }
    }
}