namespace Application.Validators;

internal class BranchValidator : AbstractValidator<BranchRequest>
{
    public BranchValidator(IRepository<Branch, int> repository, int id = 0, int branchId = 0)
    {
        //if (id != 0)
        //{
        //    RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
        //    {
        //        return await repository.Query().AnyAsync(q => q.Id == id);
        //    }).WithMessage("Branch not found with is id");
        //}
        //else
        //{
        //    if (branchId != 0)
        //    {
        //        RuleFor(cmd => cmd.BranchCode).MustAsync(async (productCode, cancellation) =>
        //        {
        //            return !await repository.Query().AnyAsync(q => q.BranchId == branchId && q.BranchCode == productCode);
        //        }).WithMessage("Branch code already used by someone ! Please re-generate the code.");
        //    }
        //    else
        //    {
        //        RuleFor(cmd => cmd.BranchCode).MustAsync(async (productCode, cancellation) =>
        //        {
        //            return !await repository.Query().AnyAsync(q => q.BranchCode == productCode);
        //        }).WithMessage("Branch code already used by someone ! Please re-generate the code.");

        //    }
        //}



        RuleFor(cmd => cmd.Name)
               .NotNull()
               .MinimumLength(1)
               .WithMessage("Branch name is mandatory");

        RuleFor(cmd => cmd.Phone)
            .NotNull()
            .WithMessage("Phone is mandatory");
    }

}