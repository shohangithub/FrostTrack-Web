namespace Application.Validators;

internal class ProductReceiveValidator : AbstractValidator<ProductReceiveRequest>
{
    public ProductReceiveValidator(IRepository<ProductReceive, long> repository, long id = 0, long branchId = 0)
    {
        if (id != 0)
        {
            RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
            {
                return await repository.Query().AnyAsync(q => q.Id == id);
            }).WithMessage("Product receive not found with this id");
        }
        else
        {
            if (branchId != 0)
            {
                RuleFor(cmd => cmd.ReceiveNumber).MustAsync(async (receiveNumber, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.BranchId == branchId && q.ReceiveNumber == receiveNumber);
                }).WithMessage("Receive number already used! Please re-generate the receive number.");
            }
            else
            {
                RuleFor(cmd => cmd.ReceiveNumber).MustAsync(async (receiveNumber, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.ReceiveNumber == receiveNumber);
                }).WithMessage("Receive number already used! Please re-generate the receive number.");
            }
        }

        RuleFor(cmd => cmd.CustomerId)
               .NotNull()
               .WithMessage("Customer is mandatory");

        RuleFor(cmd => cmd.Subtotal)
            .NotNull()
            .WithMessage("Subtotal is mandatory");

        RuleFor(cmd => cmd.TotalAmount)
            .NotNull()
            .WithMessage("Total amount is mandatory");

        RuleFor(cmd => cmd.ReceiveNumber)
            .NotNull()
            .WithMessage("Receive number is mandatory");
    }
}
