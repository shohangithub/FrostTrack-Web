using Application.RequestDTO;
using FluentValidation;

namespace Application.Validators;

public class DamageValidator : AbstractValidator<DamageRequest>
{
    private readonly IRepository<Damage, int> _repository;
    private readonly int? _id;
    private readonly int? _branchId;

    public DamageValidator(IRepository<Damage, int> repository, int? id = null, int? branchId = null)
    {
        _repository = repository;
        _id = id;
        _branchId = branchId;

        RuleFor(x => x.DamageNumber)
            .NotEmpty().WithMessage("Damage Number is required")
            .MaximumLength(50).WithMessage("Damage Number cannot exceed 50 characters")
            .MustAsync(BeUniqueDamageNumber).WithMessage("Damage Number already exists");

        RuleFor(x => x.DamageDate)
            .NotEmpty().WithMessage("Damage Date is required");

        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Product is required");

        RuleFor(x => x.UnitId)
            .GreaterThan(0).WithMessage("Unit is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("Unit Cost must be greater than or equal to 0");

        RuleFor(x => x.TotalCost)
            .GreaterThanOrEqualTo(0).WithMessage("Total Cost must be greater than or equal to 0");

        RuleFor(x => x.Reason)
            .MaximumLength(200).WithMessage("Reason cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }

    private async Task<bool> BeUniqueDamageNumber(string damageNumber, CancellationToken cancellationToken)
    {
        var query = _repository.Query().Where(x => x.DamageNumber == damageNumber);

        if (_branchId.HasValue)
        {
            query = query.Where(x => x.BranchId == _branchId.Value);
        }

        if (_id.HasValue)
        {
            query = query.Where(x => x.Id != _id.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }
}