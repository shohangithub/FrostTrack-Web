namespace Application.Validators;

internal class AssetValidator : AbstractValidator<AssetRequest>
{
    public AssetValidator(IRepository<Asset, int> repository, int id = 0, int branchId = 0)
    {
        if (id != 0)
        {
            RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
            {
                return await repository.Query().AnyAsync(q => q.Id == id);
            }).WithMessage("Asset not found with this id");
        }
        else
        {
            if (branchId != 0)
            {
                RuleFor(cmd => cmd.AssetCode).MustAsync(async (assetCode, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.BranchId == branchId && q.AssetCode == assetCode);
                }).WithMessage("Asset code already used by someone! Please re-generate the code.");
            }
            else
            {
                RuleFor(cmd => cmd.AssetCode).MustAsync(async (assetCode, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.AssetCode == assetCode);
                }).WithMessage("Asset code already used by someone! Please re-generate the code.");
            }
        }

        RuleFor(cmd => cmd.AssetName)
               .NotNull()
               .MinimumLength(1)
               .WithMessage("Asset name is mandatory");

        RuleFor(cmd => cmd.AssetCode)
               .NotNull()
               .MinimumLength(1)
               .WithMessage("Asset code is mandatory");

        RuleFor(cmd => cmd.PurchaseCost)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Purchase cost must be greater than or equal to zero");

        RuleFor(cmd => cmd.CurrentValue)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Current value must be greater than or equal to zero");

        RuleFor(cmd => cmd.DepreciationRate)
            .InclusiveBetween(0, 100)
            .WithMessage("Depreciation rate must be between 0 and 100 percent");
    }
}