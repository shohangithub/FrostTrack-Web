namespace Domain.Entitites;

[Table("Suppliers")]
public class Supplier : AuditableEntity<int>
{
    public required string SupplierName { get; set; }
    public required string SupplierCode { get; set; }
    public string SupplierBarcode { get; set; } = string.Empty;
    public string? SupplierMobile { get; set; }
    public string? SupplierEmail { get; set; }
    public string? OfficePhone { get; set; }
    public string? Address { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public decimal CreditLimit { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public decimal OpeningBalance { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public decimal PreviousDue { get; set; } = decimal.Zero;
    public string? ImageUrl { get; set; } = string.Empty;
    public bool IsSystemDefault { get; set; } = false;
    public required bool IsActive { get; set; } = true;
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";

    public int? BranchId { get; set; }
    public ICollection<Purchase> Purchases { get; set; } = [];
}
