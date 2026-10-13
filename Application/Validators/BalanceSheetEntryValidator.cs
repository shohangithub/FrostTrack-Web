using FluentValidation;

namespace Application.Validators;

internal class BalanceSheetEntryValidator : AbstractValidator<BalanceSheetEntryRequest>
{
    private static readonly string[] ProfitCategories =
    [
        AccountCategories.INCOME, AccountCategories.EXPENSE_DIRECT, AccountCategories.EXPENSE_ADMIN,
        AccountCategories.EXPENSE_FINANCE, AccountCategories.TAX
    ];

    public BalanceSheetEntryValidator()
    {
        RuleFor(x => x.EntryDate)
            .NotEmpty().WithMessage("Date is required");

        RuleFor(x => x.EntryType)
            .Must(x => x == BalanceSheetEntryTypes.OPENING || x == BalanceSheetEntryTypes.ADJUSTMENT)
            .WithMessage("Entry type must be OPENING or ADJUSTMENT");

        RuleFor(x => x.LineItem)
            .Must(x => BalanceSheetLineItems.All.Contains(x))
            .WithMessage("Invalid balance sheet line");

        RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("Account name is required")
            .MaximumLength(200).WithMessage("Account name cannot exceed 200 characters");

        RuleFor(x => x.Amount)
            .NotEqual(0).WithMessage("Amount cannot be zero");

        // An adjustment moves an asset/liability and profit together; equity lines have no profit side
        RuleFor(x => x.LineItem)
            .Must(x => !BalanceSheetLineItems.IsEquity(x))
            .When(x => x.EntryType == BalanceSheetEntryTypes.ADJUSTMENT)
            .WithMessage("Adjustments are for asset or liability lines; enter capital and retained earnings as opening entries");

        RuleFor(x => x.ProfitCategory)
            .Must(x => ProfitCategories.Contains(x))
            .When(x => x.EntryType == BalanceSheetEntryTypes.ADJUSTMENT)
            .WithMessage("Select the profit & loss category of the adjustment");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note cannot exceed 500 characters");
    }
}

internal class BalanceSheetSettingsValidator : AbstractValidator<BalanceSheetSettingsRequest>
{
    public BalanceSheetSettingsValidator()
    {
        RuleFor(x => x.FiscalYearStartMonth)
            .InclusiveBetween(1, 12).WithMessage("Fiscal year start month must be between 1 and 12");
    }
}
