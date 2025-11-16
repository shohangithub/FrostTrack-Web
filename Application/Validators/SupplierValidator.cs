namespace Application.Validators;

internal class SupplierValidator : AbstractValidator<SupplierRequest>
{
    public SupplierValidator(IRepository<Supplier, int> repository, int id = 0, int branchId = 0)
    {
        if (id != 0)
        {
            RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
            {
                return await repository.Query().AnyAsync(q => q.Id == id);
            }).WithMessage("Supplier not found with is id");
        }
        else
        {
            if (branchId != 0)
            {
                RuleFor(cmd => cmd.SupplierCode).MustAsync(async (supplierCode, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.BranchId == branchId && q.SupplierCode == supplierCode);
                }).WithMessage("Supplier code already used by someone ! Please re-generate the code.");
            }
            else
            {
                RuleFor(cmd => cmd.SupplierCode).MustAsync(async (supplierCode, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.SupplierCode == supplierCode);
                }).WithMessage("Supplier code already used by someone ! Please re-generate the code.");

            }
        }



        RuleFor(cmd => cmd.SupplierName)
               .NotNull()
               .MinimumLength(1)
               .WithMessage("Supplier name is mandatory");

        RuleFor(cmd => cmd.SupplierMobile)
            .NotNull()
            .MinimumLength(1)
            .WithMessage("Supplier mobile is mandatory");
    }

}