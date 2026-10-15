using FluentValidation;

namespace Application.Validators;

internal class TransactionGroupValidator : AbstractValidator<TransactionGroupRequest>
{
    private readonly IRepository<TransactionGroup, Guid> _repository;
    private readonly Guid? _id;

    public TransactionGroupValidator(IRepository<TransactionGroup, Guid> repository, Guid? id = null)
    {
        _repository = repository;
        _id = id;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters")
            .MustAsync(BeUniqueName).WithMessage("A transaction group with this name already exists");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.AccountType)
            .MaximumLength(100).WithMessage("Account type cannot exceed 100 characters");
    }

    private async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
    {
        var trimmed = name.Trim();
        var exists = await _repository.Query()
            .AnyAsync(x => x.Name == trimmed && (_id == null || x.Id != _id), cancellationToken);
        return !exists;
    }
}

internal class DeleteTransactionGroupValidator : AbstractValidator<Guid>
{
    private readonly IRepository<TransactionGroup, Guid> _repository;
    private readonly IRepository<TransactionHead, Guid> _headRepository;

    public DeleteTransactionGroupValidator(
        IRepository<TransactionGroup, Guid> repository,
        IRepository<TransactionHead, Guid> headRepository)
    {
        _repository = repository;
        _headRepository = headRepository;

        RuleFor(x => x)
            .MustAsync(ExistsAsync).WithMessage("Transaction group not found")
            .MustAsync(NotBeInUse).WithMessage("Cannot delete this group because transaction heads are mapped to it. Remove the mapping first.");
    }

    private async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);
    }

    private async Task<bool> NotBeInUse(Guid id, CancellationToken cancellationToken)
    {
        return !await _headRepository.Query().AnyAsync(x => x.TransactionGroupId == id, cancellationToken);
    }
}
