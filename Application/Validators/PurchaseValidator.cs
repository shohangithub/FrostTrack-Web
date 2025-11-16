namespace Application.Validators;

internal class PurchaseValidator : AbstractValidator<PurchaseRequest>
{
    public PurchaseValidator(IRepository<Purchase, long> repository, long id = 0, long branchId = 0)
    {
        if (id != 0)
        {
            RuleFor(cmd => cmd).MustAsync(async (name, cancellation) =>
            {
                return await repository.Query().AnyAsync(q => q.Id == id);
            }).WithMessage("Purchase not found with is id");
        }
        else
        {
            if (branchId != 0)
            {
                RuleFor(cmd => cmd.InvoiceNumber).MustAsync(async (invoiceNumber, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.BranchId == branchId && q.InvoiceNumber == invoiceNumber);
                }).WithMessage("Invoice number already used by someone ! Please re-generate the invoice number.");
            }
            else
            {
                RuleFor(cmd => cmd.InvoiceNumber).MustAsync(async (invoiceNumber, cancellation) =>
                {
                    return !await repository.Query().AnyAsync(q => q.InvoiceNumber == invoiceNumber);
                }).WithMessage("Invoice number already used by someone ! Please re-generate the invoice number.");

            }
        }



        RuleFor(cmd => cmd.SupplierId)
               .NotNull()
               .WithMessage("Supplier is mandatory");

        //RuleFor(cmd => cmd.ProductId)
        //    .NotNull()
        //    .WithMessage("Product is mandatory");

        RuleFor(cmd => cmd.Subtotal)
            .NotNull()
            .WithMessage("Subtotal is mandatory");
        RuleFor(cmd => cmd.InvoiceAmount)
            .NotNull()
            .WithMessage("Invoice amount is mandatory");

        RuleFor(cmd => cmd.InvoiceNumber)
            .NotNull()
            .WithMessage("Invoice number is mandatory");
    }

}