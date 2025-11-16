namespace Domain.Entitites;

[Table("Assets")]
public class Asset : AuditableEntity<int>
{
    public required string AssetName { get; set; }
    public required string AssetCode { get; set; }
    public string? AssetType { get; set; }
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public DateTime? PurchaseDate { get; set; }
    [Column(TypeName = "decimal(18, 2)")]
    public decimal PurchaseCost { get; set; }
    [Column(TypeName = "decimal(18, 2)")]
    public decimal CurrentValue { get; set; }
    [Column(TypeName = "decimal(5, 2)")]
    public decimal DepreciationRate { get; set; }
    public string? Location { get; set; }
    public string? Department { get; set; }
    public string? AssignedTo { get; set; }
    public string? Condition { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public DateTime? MaintenanceDate { get; set; }
    public string? Notes { get; set; }
    public string? ImageUrl { get; set; }
    public required bool IsActive { get; set; } = true;
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";

    public int? BranchId { get; set; }

    // Navigation properties for future asset transactions
    // public ICollection<AssetTransaction> AssetTransactions { get; set; } = [];
}