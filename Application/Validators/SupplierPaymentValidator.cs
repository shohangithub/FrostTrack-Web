namespace Application.Validators;

internal class SupplierPaymentValidator : AbstractValidator<SupplierPaymentRequest>
{
    public SupplierPaymentValidator(IRepository<SupplierPayment, long> repository, long id = 0, long branchId = 0)
    {
        if (id != 0)
        {
            RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
            {
                return await repository.Query().AnyAsync(q => q.Id == id);
            }).WithMessage("Supplier Payment not found with is id");
        }
        else
        {
            if (branchId != 0)
            {
                RuleFor(cmd => cmd.PaymentNumber).MustAsync(async (paymentNumber, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.BranchId == branchId && q.PaymentNumber == paymentNumber);
                }).WithMessage("Payment number already used by someone ! Please re-generate the payment number.");
            }
            else
            {
                RuleFor(cmd => cmd.PaymentNumber).MustAsync(async (paymentNumber, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.PaymentNumber == paymentNumber);
                }).WithMessage("Payment number already used by someone ! Please re-generate the payment number.");
            }
        }

        RuleFor(cmd => cmd.PaymentType)
               .NotEmpty()
               .WithMessage("Payment type is mandatory");

        RuleFor(cmd => cmd).Must(cmd => cmd.SupplierId.HasValue || cmd.CustomerId.HasValue)
               .WithMessage("Either Supplier or Customer must be selected");

        RuleFor(cmd => cmd).Must(cmd => !(cmd.SupplierId.HasValue && cmd.CustomerId.HasValue))
               .WithMessage("Cannot select both Supplier and Customer");

        RuleFor(cmd => cmd.PaymentMethod)
               .NotEmpty()
               .WithMessage("Payment method is mandatory");

        RuleFor(cmd => cmd.PaymentAmount)
            .NotNull()
            .GreaterThan(0)
            .WithMessage("Payment amount must be greater than 0");

        RuleFor(cmd => cmd.PaymentNumber)
            .NotEmpty()
            .WithMessage("Payment number is mandatory");

        RuleFor(cmd => cmd.PaymentDate)
            .NotNull()
            .WithMessage("Payment date is mandatory");

        // Bank validation when payment method is Bank or Check
        RuleFor(cmd => cmd.BankId)
            .NotNull()
            .When(cmd => cmd.PaymentMethod == "Bank" || cmd.PaymentMethod == "Check")
            .WithMessage("Bank selection is mandatory for Bank/Check payments");

        // Check number validation when payment method is Check
        RuleFor(cmd => cmd.CheckNumber)
            .NotEmpty()
            .When(cmd => cmd.PaymentMethod == "Check")
            .WithMessage("Check number is mandatory for Check payments");

        RuleFor(cmd => cmd.CheckDate)
            .NotNull()
            .When(cmd => cmd.PaymentMethod == "Check")
            .WithMessage("Check date is mandatory for Check payments");
    }
}