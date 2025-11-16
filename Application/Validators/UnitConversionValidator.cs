namespace Application.Validators;

internal class UnitConversionValidator : AbstractValidator<UnitConversionRequest>
{
    public UnitConversionValidator(IRepository<UnitConversion, int> repository, int id = 0)
    {
        if (id != 0)
        {
            RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
            {
                return await repository.Query().AnyAsync(q => q.Id == id);
            }).WithMessage("Unit not found with is id");

            RuleFor(cmd => cmd.UnitName).NotNull().MinimumLength(1).MustAsync(async (name, cancellation) =>
            {
                return !await repository.Query().AnyAsync(q => q.UnitName.ToLower() == name.ToLower() && q.Id != id );
            }).WithMessage("Unit Name must be unique");
        }
        else
        {
            RuleFor(cmd => cmd.UnitName).NotNull().MinimumLength(1).MustAsync(async (name, cancellation) =>
            {
                return !await repository.Query().AnyAsync(q => q.UnitName.ToLower() == name.ToLower());
            }).WithMessage("Unit Name must be unique");
        }
    }

}