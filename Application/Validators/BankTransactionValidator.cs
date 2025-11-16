namespace Application.Validators;

public class BankTransactionValidator : AbstractValidator<BankTransactionRequest>
{
    private readonly IRepository<BankTransaction, long> _repository;

    public BankTransactionValidator(IRepository<BankTransaction, long> repository, long? id = null, int? branchId = null)
    {
        _repository = repository;

        RuleFor(x => x.TransactionNumber)
            .NotEmpty().WithMessage("Transaction Number is required")
            .MustAsync(async (model, transactionNumber, cancellation) =>
                await BeUniqueTransactionNumber(model, transactionNumber, id, branchId, cancellation))
            .WithMessage("Transaction Number must be unique");

        RuleFor(x => x.TransactionDate)
            .NotEmpty().WithMessage("Transaction Date is required");

        RuleFor(x => x.BankId)
            .NotEmpty().WithMessage("Bank is required")
            .GreaterThan(0).WithMessage("Bank is required");

        RuleFor(x => x.TransactionType)
            .NotEmpty().WithMessage("Transaction Type is required")
            .Must(x => x == "Deposit" || x == "Withdraw")
            .WithMessage("Transaction Type must be either 'Deposit' or 'Withdraw'");

        RuleFor(x => x.Amount)
            .NotEmpty().WithMessage("Amount is required")
            .GreaterThan(0).WithMessage("Amount must be greater than 0");
    }

    private async Task<bool> BeUniqueTransactionNumber(BankTransactionRequest model, string transactionNumber, long? id, int? branchId, CancellationToken cancellationToken)
    {
        if (branchId.HasValue)
        {
            return !await _repository.Query()
                .Where(x => x.BranchId == branchId)
                .Where(x => x.Id != id)
                .AnyAsync(x => x.TransactionNumber == transactionNumber, cancellationToken);
        }
        else
        {
            return !await _repository.Query()
                .Where(x => x.Id != id)
                .AnyAsync(x => x.TransactionNumber == transactionNumber, cancellationToken);
        }
    }
}