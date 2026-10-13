namespace Domain.Entitites;

/// <summary>
/// Balance Sheet figures that are not cash movements: the opening position from the last audited
/// balance sheet (share capital, creditors, deposits, provisions, retained earnings b/f) and year-end
/// adjustments (provisions etc.). Never posted to the cash book or bank book.
/// </summary>
[Table("BalanceSheetEntries", Schema = "finance")]
public class BalanceSheetEntry : AuditableEntity<Guid>
{
    [Required]
    [MaxLength(100)]
    public required string Code { get; set; }

    /// <summary>Calendar date; the entry counts in balance sheets on or after it.</summary>
    [Column(TypeName = "datetime2")]
    public DateTime EntryDate { get; set; }

    /// <summary>OPENING | ADJUSTMENT (see BalanceSheetEntryTypes).</summary>
    [Required]
    [MaxLength(20)]
    public required string EntryType { get; set; }

    /// <summary>Balance Sheet line the amount adds to (see BalanceSheetLineItems).</summary>
    [Required]
    [MaxLength(50)]
    public required string LineItem { get; set; }

    [Required]
    [MaxLength(200)]
    public required string AccountName { get; set; }

    /// <summary>Signed: positive increases the line, negative reduces it.</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    /// <summary>ADJUSTMENT only: P&amp;L side of the entry (an AccountCategories expense/income value).</summary>
    [MaxLength(50)]
    public string? ProfitCategory { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}

public static class BalanceSheetEntryTypes
{
    public const string OPENING = "OPENING";       // position on the start date; other side is the opening equity
    public const string ADJUSTMENT = "ADJUSTMENT"; // year-end non-cash entry; other side is profit
}

public static class BalanceSheetLineItems
{
    // Assets
    public const string ADVANCE_DEPOSIT = "ADVANCE_DEPOSIT";
    public const string OTHER_ASSET = "OTHER_ASSET";
    // Liabilities
    public const string SUNDRY_CREDITORS = "SUNDRY_CREDITORS";
    public const string PROVISION = "PROVISION";
    public const string OTHER_LIABILITY = "OTHER_LIABILITY";
    // Equity
    public const string SHARE_CAPITAL = "SHARE_CAPITAL";
    public const string RETAINED_EARNINGS = "RETAINED_EARNINGS";

    public static readonly string[] Assets = [ADVANCE_DEPOSIT, OTHER_ASSET];
    public static readonly string[] Liabilities = [SUNDRY_CREDITORS, PROVISION, OTHER_LIABILITY];
    public static readonly string[] Equity = [SHARE_CAPITAL, RETAINED_EARNINGS];
    public static readonly string[] All = [.. Assets, .. Liabilities, .. Equity];

    public static bool IsAsset(string lineItem) => Assets.Contains(lineItem);
    public static bool IsEquity(string lineItem) => Equity.Contains(lineItem);
}
