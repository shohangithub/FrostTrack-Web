namespace Application.Validators;

internal class ProductValidator : AbstractValidator<ProductRequest>
{
    public ProductValidator(IRepository<Product, int> repository, int id = 0, int branchId = 0)
    {
        if (id != 0)
        {
            RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
            {
                return await repository.Query().AnyAsync(q => q.Id == id);
            }).WithMessage("Product not found with is id");
        }
        else
        {
            if (branchId != 0)
            {
                RuleFor(cmd => cmd.ProductCode).MustAsync(async (productCode, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.BranchId == branchId && q.ProductCode == productCode);
                }).WithMessage("Product code already used by someone ! Please re-generate the code.");
            }
            else
            {
                RuleFor(cmd => cmd.ProductCode).MustAsync(async (productCode, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.ProductCode == productCode);
                }).WithMessage("Product code already used by someone ! Please re-generate the code.");

            }
        }



        RuleFor(cmd => cmd.ProductName)
               .NotNull()
               .MinimumLength(1)
               .WithMessage("Product name is mandatory");

        RuleFor(cmd => cmd.CategoryId)
            .NotNull()
            .WithMessage("Product category is mandatory");
    }

}