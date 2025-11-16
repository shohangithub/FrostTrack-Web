namespace Application.Validators;

internal class BaseUnitValidator : AbstractValidator<BaseUnitRequest>
{
    public BaseUnitValidator(IRepository<BaseUnit, int> repository, int id = 0)
    {
        if (id != 0)
        {
            RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
            {
                return await repository.Query().AnyAsync(q => q.Id == id);
            }).WithMessage("Unit not found with is id");

            RuleFor(cmd => cmd.UnitName).NotNull().MinimumLength(1).MustAsync(async (name, cancellation) =>
            {
                return !await repository.Query().AnyAsync(q => q.UnitName.ToLower() == name.ToLower() && q.Id != id);
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


internal class DeleteBaseUnitValidator : AbstractValidator<int>
{
    public DeleteBaseUnitValidator(IRepository<BaseUnit, int> repository, IRepository<UnitConversion, int> unitConversionrepository, int id)
    {

        RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
        {
            return await repository.Query().AnyAsync(q => q.Id == id);
        }).WithMessage("Unit not found with is id");

        RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
        {
            var result = await unitConversionrepository.Query().Where(q => q.BaseUnit.Id == id).CountAsync() > 1;
            return !result;
        }).WithMessage("Already you have used this unit. Remove unit conversion first.");
    }

}