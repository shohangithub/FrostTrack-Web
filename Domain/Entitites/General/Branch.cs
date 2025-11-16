namespace Domain.Entitites;

[Table("Branches")]
public class Branch : BaseEntity<int>
{
    public required string Name { get; set; }
    public required string BranchCode { get; set; }
    public string BusinessCurrency { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool AutoInvoicePrint { get; set; } = true;
    public string InvoiceHeader { get; set; } = string.Empty;
    public string InvoiceFooter { get; set; } = string.Empty;
    public bool IsMainBranch { get; set; }
    public required bool IsActive { get; set; } = true;
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";
   // public ICollection<ApplicationUser> Users { get; set; } = [];
    public ICollection<Purchase> Purchases { get; set; } = [];
    public ICollection<Sales> Sales { get; set; } = [];
}