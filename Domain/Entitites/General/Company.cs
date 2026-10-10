namespace Domain.Entitites;

[Table("Companies")]
public class Company : BaseEntity<int>
{
    public required string Name { get; set; }
    public string LogoUrl { get; set; } = string.Empty;
    public string BusinessCurrency { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool AutoInvoicePrint { get; set; } = true;
    public bool AutoGenerateBookingNo { get; set; } = false;
    public bool EnableRecurringChargeJob { get; set; } = false;
    public bool EnableEmailNotifications { get; set; } = false;
    // Booking / delivery / transaction edit & delete is allowed only within this many days of the record date
    public bool RestrictEditDelete { get; set; } = true;
    public int EditDeleteAllowedDays { get; set; } = 90;
    /// <summary>
    /// Go-live date (calendar date). Billing months that ended before it and labour dated before it are
    /// not posted to the charge ledger (settled before the system). The month running on the go-live
    /// date is charged, dated on the go-live date. Null = charge from each booking's own date.
    /// </summary>
    public DateTime? BillingStartDate { get; set; }
    public string InvoiceHeader { get; set; } = string.Empty;
    public string InvoiceFooter { get; set; } = string.Empty;
    public bool IsSingleBranch { get; set; }
    public ECodeGeneration CodeGeneration { get; set; } = ECodeGeneration.Company;
    public required bool IsActive { get; set; } = true;
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";
    public ICollection<Branch> Branches { get; set; } = [];
}