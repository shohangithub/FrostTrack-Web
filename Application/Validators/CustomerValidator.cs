namespace Application.Validators;

internal class CustomerValidator : AbstractValidator<CustomerRequest>
{
    public CustomerValidator(IRepository<Customer, int> repository, int id = 0, int branchId = 0)
    {
        if (id != 0)
        {
            RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
            {
                return await repository.Query().AnyAsync(q => q.Id == id);
            }).WithMessage("Customer not found with is id");
        }
        else
        {
            if (branchId != 0)
            {
                RuleFor(cmd => cmd.CustomerCode).MustAsync(async (supplierCode, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.BranchId == branchId && q.CustomerCode == supplierCode);
                }).WithMessage("Customer code already used by someone ! Please re-generate the code.");
            }
            else
            {
                RuleFor(cmd => cmd.CustomerCode).MustAsync(async (supplierCode, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.CustomerCode == supplierCode);
                }).WithMessage("Customer code already used by someone ! Please re-generate the code.");

            }
        }



        RuleFor(cmd => cmd.CustomerName)
               .NotNull()
               .MinimumLength(1)
               .WithMessage("Customer name is mandatory");

        RuleFor(cmd => cmd.CustomerMobile)
            .NotNull()
            .MinimumLength(1)
            .WithMessage("Customer mobile is mandatory");
    }

}