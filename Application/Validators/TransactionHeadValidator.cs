using FluentValidation;

namespace Application.Validators;

internal class TransactionHeadValidator : AbstractValidator<TransactionHeadRequest>
{
    private readonly IRepository<TransactionHead, Guid> _repository;
    private readonly Guid? _id;

    public TransactionHeadValidator(IRepository<TransactionHead, Guid> repository, Guid? id = null)
    {
        _repository = repository;
        _id = id;

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(100).WithMessage("Code cannot exceed 100 characters")
            .MustAsync(BeUniqueCode).WithMessage("Code already exists");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required")
            .Must(x => x == TransactionHeadTypes.DEBIT || x == TransactionHeadTypes.CREDIT)
            .WithMessage("Type must be either DEBIT or CREDIT");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }

    private async Task<bool> BeUniqueCode(string code, CancellationToken cancellationToken)
    {
        var exists = await _repository.Query()
            .AnyAsync(x => x.Code == code && (_id == null || x.Id != _id), cancellationToken);
        return !exists;
    }
}

internal class DeleteTransactionHeadValidator : AbstractValidator<Guid>
{
    private readonly IRepository<TransactionHead, Guid> _repository;
    private readonly Guid _id;

    public DeleteTransactionHeadValidator(IRepository<TransactionHead, Guid> repository, Guid id)
    {
        _repository = repository;
        _id = id;

        RuleFor(x => x)
            .MustAsync(ExistsAsync).WithMessage("Transaction head not found")
            .MustAsync(NotBeSystemHead).WithMessage("Cannot delete system transaction heads");
    }

    private async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);
    }

    private async Task<bool> NotBeSystemHead(Guid id, CancellationToken cancellationToken)
    {
        var head = await _repository.GetByIdAsync(id, cancellationToken);
        return head != null && !head.IsSystem;
    }
}
