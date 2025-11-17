namespace Domain.Entitites;

[Table("Products", Schema = "product")]
public class Product : AuditableEntity<int>
{
    public required string ProductName { get; set; }
    public required string ProductCode { get; set; }
    public string CustomBarcode { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public ProductCategory? Category { get; set; }
    public int? DefaultUnitId { get; set; }
    public UnitConversion? DefaultUnit { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? BookingRate { get; set; }
    public required bool IsActive { get; set; } = true;
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";

    public int? BranchId { get; set; }

    public ICollection<PurchaseDetail> PurchaseDetails { get; set; } = [];
    public ICollection<SalesDetail> SalesDetails { get; set; } = [];
    public Stock Stock { get; set; }
}
