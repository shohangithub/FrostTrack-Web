using FluentValidation;

namespace Application.Validators;

internal class TransactionHeadValidator : AbstractValidator<TransactionHeadRequest>
{
    private readonly IRepository<TransactionHead, Guid> _repository;
    private readonly IRepository<TransactionGroup, Guid> _groupRepository;
    private readonly Guid? _id;

    public TransactionHeadValidator(
        IRepository<TransactionHead, Guid> repository,
        IRepository<TransactionGroup, Guid> groupRepository,
        Guid? id = null)
    {
        _repository = repository;
        _groupRepository = groupRepository;
        _id = id;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required")
            .Must(x => x == TransactionHeadTypes.DEBIT || x == TransactionHeadTypes.CREDIT)
            .WithMessage("Type must be either DEBIT or CREDIT");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.AccountCategory)
            .Must(x => AccountCategories.All.Contains(x))
            .WithMessage("Invalid account category")
            .When(x => !string.IsNullOrEmpty(x.AccountCategory));

        RuleFor(x => x.TransactionGroupId)
            .MustAsync(GroupExists).WithMessage("Selected transaction group not found")
            .When(x => x.TransactionGroupId.HasValue);
    }

    private async Task<bool> GroupExists(Guid? groupId, CancellationToken cancellationToken)
    {
        return await _groupRepository.Query().AnyAsync(x => x.Id == groupId, cancellationToken);
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
