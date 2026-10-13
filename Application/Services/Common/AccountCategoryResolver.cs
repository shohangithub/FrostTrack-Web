namespace Application.Services.Common;

/// <summary>
/// Where a transaction head's amounts go in the Balance Sheet / P&amp;L. System usages are fixed;
/// other heads use their AccountCategory, or INCOME (money in) / EXPENSE_ADMIN (money out) when it is not set.
/// </summary>
public static class AccountCategoryResolver
{
    /// <summary>Not profit and not a balance sheet line: opening/closing cash and customer payments (they settle receivables).</summary>
    public const string Excluded = "EXCLUDED";

    /// <summary>Bill discounts: reduce rent income (and the customer's due).</summary>
    public const string Discount = "DISCOUNT";

    public static string Resolve(string? accountCategory, string type, string usageFor)
    {
        if (usageFor == UsageFor.OPENING_BALANCE || usageFor == UsageFor.CLOSING_BALANCE) return Excluded;
        if (usageFor == UsageFor.BILL_DISCOUNT) return Discount;
        if (usageFor == UsageFor.CUSTOMER_PAYMENT && type == TransactionHeadTypes.DEBIT) return Excluded;
        if (!string.IsNullOrEmpty(accountCategory)) return accountCategory;
        return type == TransactionHeadTypes.DEBIT ? AccountCategories.INCOME : AccountCategories.EXPENSE_ADMIN;
    }

    public static bool IsProfitCategory(string category) =>
        category is AccountCategories.INCOME or AccountCategories.EXPENSE_DIRECT or AccountCategories.EXPENSE_ADMIN
            or AccountCategories.EXPENSE_FINANCE or AccountCategories.TAX;
}
