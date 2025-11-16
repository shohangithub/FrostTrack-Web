namespace Application.Validators;

internal class UserValidator : AbstractValidator<UserRequest>
{
    public UserValidator(IRepository<ApplicationUser, int> repository)
    {
        RuleFor(cmd => cmd.UserName).NotNull().MinimumLength(3);
        RuleFor(cmd => cmd.Email).NotNull().EmailAddress()
            .MustAsync(async (email, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.Email == email);
                })
            .WithMessage("Email Address must be unique");
    }

}