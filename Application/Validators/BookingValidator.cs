namespace Application.Validators;

internal class BookingValidator : AbstractValidator<BookingRequest>
{
    public BookingValidator(IRepository<Booking, Guid> repository, Guid id = default, long branchId = 0)
    {
        if (id != Guid.Empty)
        {
            RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
            {
                return await repository.Query().AnyAsync(q => q.Id == id);
            }).WithMessage("Booking not found with this id");
        }
        else
        {
            if (branchId != 0)
            {
                RuleFor(cmd => cmd.BookingNumber).MustAsync(async (bookingNumber, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.BranchId == branchId && q.BookingNumber == bookingNumber);
                }).WithMessage("Booking number already used! Please re-generate the booking number.");
            }
            else
            {
                RuleFor(cmd => cmd.BookingNumber).MustAsync(async (bookingNumber, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.BookingNumber == bookingNumber);
                }).WithMessage("Booking number already used! Please re-generate the booking number.");
            }
        }

        RuleFor(cmd => cmd.CustomerId)
               .NotNull()
               .WithMessage("Customer is mandatory");

        RuleFor(cmd => cmd.BookingNumber)
            .NotNull()
            .WithMessage("Booking number is mandatory");
    }
}
