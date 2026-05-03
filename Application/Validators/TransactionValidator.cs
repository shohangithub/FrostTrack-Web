using FluentValidation;

namespace Application.Validators;

public class TransactionValidator : AbstractValidator<TransactionRequest>
{
    private readonly IRepository<Transaction, Guid> _repository;
    private readonly IRepository<TransactionHead, Guid> _transactionHeadRepository;
    private readonly Guid? _currentId;

    public TransactionValidator(
        IRepository<Transaction, Guid> repository,
        IRepository<TransactionHead, Guid> transactionHeadRepository,
        Guid? currentId = null)
    {
        _repository = repository;
        _transactionHeadRepository = transactionHeadRepository;
        _currentId = currentId;

        RuleFor(x => x.TransactionCode)
            .NotEmpty().WithMessage("Transaction code is required")
            .MaximumLength(50).WithMessage("Transaction code cannot exceed 50 characters")
            .MustAsync(BeUniqueTransactionCode).WithMessage("Transaction code already exists");

        RuleFor(x => x.TransactionDate)
            .NotEmpty().WithMessage("Transaction date is required");

        RuleFor(x => x.TransactionHeadId)
            .NotEqual(Guid.Empty).WithMessage("Transaction head is required")
            .MustAsync(BeValidTransactionHead).WithMessage("Invalid transaction head selected");

        RuleFor(x => x.BranchId)
            .GreaterThan(0).WithMessage("Branch is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0");

        RuleFor(x => x.NetAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Net amount cannot be negative");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required")
            .Must(BeValidPaymentMethod).WithMessage("Invalid payment method");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters");
    }

    private async Task<bool> BeUniqueTransactionCode(string transactionCode, CancellationToken cancellationToken)
    {
        var exists = await _repository.Query()
            .AnyAsync(x => x.TransactionCode == transactionCode && (_currentId == null || x.Id != _currentId), cancellationToken);
        return !exists;
    }

    private async Task<bool> BeValidTransactionHead(Guid transactionHeadId, CancellationToken cancellationToken)
    {
        var exists = await _transactionHeadRepository.Query()
            .AnyAsync(x => x.Id == transactionHeadId && x.IsActive, cancellationToken);
        return exists;
    }

    private bool BeValidPaymentMethod(string paymentMethod)
    {
        return paymentMethod == PaymentMethods.CASH ||
               paymentMethod == PaymentMethods.BANK_TRANSFER ||
               paymentMethod == PaymentMethods.CHEQUE ||
               paymentMethod == PaymentMethods.CARD ||
               paymentMethod == PaymentMethods.MOBILE_BANKING ||
               paymentMethod == PaymentMethods.CREDIT;
    }
}
