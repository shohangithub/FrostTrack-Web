namespace Application.Validators;

internal class ProductCategoryValidator : AbstractValidator<ProductCategoryRequest>
{
    public ProductCategoryValidator(IRepository<ProductCategory, int> repository, int id = 0)
    {
        if (id != 0)
        {
            RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
            {
                return await repository.Query().AnyAsync(q => q.Id == id);
            }).WithMessage("Category not found with is id");

            RuleFor(cmd => cmd.CategoryName).NotNull().MinimumLength(1).MustAsync(async (name, cancellation) =>
            {
                return !await repository.Query().AnyAsync(q => q.CategoryName.ToLower() == name.ToLower() && q.Id != id );
            }).WithMessage("Category Name must be unique");
        }
        else
        {
            RuleFor(cmd => cmd.CategoryName).NotNull().MinimumLength(1).MustAsync(async (name, cancellation) =>
            {
                return !await repository.Query().AnyAsync(q => q.CategoryName.ToLower() == name.ToLower());
            }).WithMessage("Category Name must be unique");
        }
    }

}