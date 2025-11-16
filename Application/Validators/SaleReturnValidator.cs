namespace Application.Validators;

internal class SaleReturnValidator : AbstractValidator<SaleReturnRequest>
{
    public SaleReturnValidator(IRepository<SaleReturn, long> repository, long id = 0, long branchId = 0)
    {
        if (id != 0)
        {
            RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
            {
                return await repository.Query().AnyAsync(q => q.Id == id);
            }).WithMessage("Sale Return not found with this id");
        }
        else
        {
            if (branchId != 0)
            {
                RuleFor(cmd => cmd.ReturnNumber).MustAsync(async (returnNumber, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.BranchId == branchId && q.ReturnNumber == returnNumber);
                }).WithMessage("Return number already used by someone ! Please re-generate the return number.");
            }
            else
            {
                RuleFor(cmd => cmd.ReturnNumber).MustAsync(async (returnNumber, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.ReturnNumber == returnNumber);
                }).WithMessage("Return number already used by someone ! Please re-generate the return number.");
            }
        }

        RuleFor(cmd => cmd.SalesId)
               .NotNull()
               .WithMessage("Original Sales is mandatory");

        RuleFor(cmd => cmd.CustomerId)
               .NotNull()
               .WithMessage("Customer is mandatory");

        RuleFor(cmd => cmd.Subtotal)
            .NotNull()
            .WithMessage("Subtotal is mandatory");

        RuleFor(cmd => cmd.ReturnAmount)
            .NotNull()
            .WithMessage("Return amount is mandatory");

        RuleFor(cmd => cmd.ReturnNumber)
            .NotNull()
            .NotEmpty()
            .WithMessage("Return number is mandatory");

        RuleFor(cmd => cmd.ReturnDate)
            .NotNull()
            .WithMessage("Return date is mandatory");

        RuleFor(cmd => cmd.SaleReturnDetails)
            .NotNull()
            .NotEmpty()
            .WithMessage("At least one return item is required");
    }
}