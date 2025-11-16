namespace Application.Validators.PaymentMethod;

internal class PaymentMethodValidator : AbstractValidator<PaymentMethodRequest>
{
    public PaymentMethodValidator(IRepository<Domain.Entitites.PaymentMethod, int> repository, int id = 0, int branchId = 0)
    {
        if (id != 0)
        {
            RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
            {
                return await repository.Query().AnyAsync(q => q.Id == id);
            }).WithMessage("Payment Method not found with this id");

            RuleFor(cmd => cmd.MethodName).NotNull().MinimumLength(1).MustAsync(async (name, cancellation) =>
            {
                return !await repository.Query().AnyAsync(q => q.MethodName.ToLower() == name.ToLower() && q.Id != id);
            }).WithMessage("Payment Method Name must be unique");

            RuleFor(cmd => cmd.Code).MustAsync(async (code, cancellation) =>
            {
                return !await repository.Query().AnyAsync(q => q.Code == code && q.Id != id);
            }).WithMessage("Payment Method Code must be unique");
        }
        else
        {
            if (branchId != 0)
            {
                RuleFor(cmd => cmd.Code).MustAsync(async (code, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.BranchId == branchId && q.Code == code);
                }).WithMessage("Payment Method code already used by someone! Please re-generate the code.");
            }
            else
            {
                RuleFor(cmd => cmd.Code).MustAsync(async (code, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.Code == code);
                }).WithMessage("Payment Method code already used by someone! Please re-generate the code.");
            }

            RuleFor(cmd => cmd.MethodName).NotNull().MinimumLength(1).MustAsync(async (name, cancellation) =>
            {
                return !await repository.Query().AnyAsync(q => q.MethodName.ToLower() == name.ToLower());
            }).WithMessage("Payment Method Name must be unique");
        }

        RuleFor(cmd => cmd.MethodName)
               .NotNull()
               .MinimumLength(1)
               .WithMessage("Payment Method name is mandatory");

        RuleFor(cmd => cmd.Code)
               .NotNull()
               .MinimumLength(1)
               .WithMessage("Payment Method code is mandatory");

        RuleFor(cmd => cmd.Category)
               .NotNull()
               .MinimumLength(1)
               .WithMessage("Payment Method category is mandatory");
    }
}