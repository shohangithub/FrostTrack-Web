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
    public string InvoiceHeader { get; set; } = string.Empty;
    public string InvoiceFooter { get; set; } = string.Empty;
    public bool IsSingleBranch { get; set; }
    public ECodeGeneration CodeGeneration { get; set; } = ECodeGeneration.Company;
    public required bool IsActive { get; set; } = true;
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";
    public ICollection<Branch> Branches { get; set; } = [];
}