using FluentValidation;

namespace Application.Validators;

public class TransactionValidator : AbstractValidator<TransactionRequest>
{
    private readonly IRepository<Transaction, Guid> _repository;
    private readonly Guid? _currentId;

    public TransactionValidator(IRepository<Transaction, Guid> repository, Guid? currentId = null)
    {
        _repository = repository;
        _currentId = currentId;

        RuleFor(x => x.TransactionCode)
            .NotEmpty().WithMessage("Transaction code is required")
            .MaximumLength(50).WithMessage("Transaction code cannot exceed 50 characters")
            .MustAsync(BeUniqueTransactionCode).WithMessage("Transaction code already exists");

        RuleFor(x => x.TransactionDate)
            .NotEmpty().WithMessage("Transaction date is required");

        RuleFor(x => x.TransactionType)
            .NotEmpty().WithMessage("Transaction type is required")
            .Must(BeValidTransactionType).WithMessage("Invalid transaction type");

        RuleFor(x => x.TransactionFlow)
            .NotEmpty().WithMessage("Transaction flow is required")
            .Must(BeValidTransactionFlow).WithMessage("Invalid transaction flow. Must be IN or OUT");

        RuleFor(x => x.EntityName)
            .NotEmpty().WithMessage("Entity name is required")
            .MaximumLength(100).WithMessage("Entity name cannot exceed 100 characters");

        RuleFor(x => x.EntityId)
            .NotEmpty().WithMessage("Entity ID is required");

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

        RuleFor(x => x.VendorName)
            .MaximumLength(200).WithMessage("Vendor name cannot exceed 200 characters");

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Customer is required")
            .When(x => x.TransactionType == TransactionTypes.BILL_COLLECTION);
    }

    private async Task<bool> BeUniqueTransactionCode(string transactionCode, CancellationToken cancellationToken)
    {
        var exists = await _repository.Query()
            .AnyAsync(x => x.TransactionCode == transactionCode && (_currentId == null || x.Id != _currentId), cancellationToken);
        return !exists;
    }

    private bool BeValidTransactionType(string transactionType)
    {
        return transactionType == TransactionTypes.BILL_COLLECTION ||
               transactionType == TransactionTypes.OFFICE_COST ||
               transactionType == TransactionTypes.BILL_PAYMENT ||
               transactionType == TransactionTypes.ADJUSTMENT ||
               transactionType == TransactionTypes.REFUND;
    }

    private bool BeValidTransactionFlow(string transactionFlow)
    {
        return transactionFlow == TransactionFlows.IN || transactionFlow == TransactionFlows.OUT;
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
