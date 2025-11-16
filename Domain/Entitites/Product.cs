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
    public required bool IsRawMaterial { get; set; }
    public required bool IsFinishedGoods { get; set; }
    public int? ReOrederLevel { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public decimal? PurchaseRate { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public decimal? SellingRate { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public decimal? WholesalePrice { get; set; }
    [Column(TypeName = "decimal(5, 2)")]
    public decimal? VatPercent { get; set; }
    public required bool IsProductAsService { get; set; } = false;
    [NotMapped]
    public string ProductAs => IsProductAsService ? "Service" : "Product";

    public required bool IsActive { get; set; } = true;
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";

    public int? BranchId { get; set; }

    public ICollection<PurchaseDetail> PurchaseDetails { get; set; } = [];
    public ICollection<SalesDetail> SalesDetails { get; set; } = [];
    public Stock Stock { get; set; }
}
