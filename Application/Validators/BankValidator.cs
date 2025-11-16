namespace Application.Validators;

internal class BankValidator : AbstractValidator<BankRequest>
{
    public BankValidator(IRepository<Bank, int> repository, int id = 0, int branchId = 0)
    {
        if (id != 0)
        {
            RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
            {
                return await repository.Query().AnyAsync(q => q.Id == id);
            }).WithMessage("Bank not found with this id");
        }
        else
        {
            if (branchId != 0)
            {
                RuleFor(cmd => cmd.BankCode).MustAsync(async (bankCode, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.BranchId == branchId && q.BankCode == bankCode);
                }).WithMessage("Bank code already used by someone! Please re-generate the code.");
            }
            else
            {
                RuleFor(cmd => cmd.BankCode).MustAsync(async (bankCode, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.BankCode == bankCode);
                }).WithMessage("Bank code already used by someone! Please re-generate the code.");
            }
        }

        RuleFor(cmd => cmd.BankName)
               .NotNull()
               .MinimumLength(1)
               .WithMessage("Bank name is mandatory");

        RuleFor(cmd => cmd.BankCode)
               .NotNull()
               .MinimumLength(1)
               .WithMessage("Bank code is mandatory");

        RuleFor(cmd => cmd.AccountNumber)
            .NotNull()
            .MinimumLength(1)
            .WithMessage("Account number is mandatory");

        RuleFor(cmd => cmd.AccountTitle)
            .NotNull()
            .MinimumLength(1)
            .WithMessage("Account title is mandatory");
    }
}