namespace Domain.Entitites;

[Table("SaleReturnDetails", Schema = "product")]
public class SaleReturnDetail : AuditableEntity<long>
{
    public required long SaleReturnId { get; set; }
    public SaleReturn SaleReturn { get; set; } = null!;
    public required int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public required int ReturnUnitId { get; set; }
    public UnitConversion ReturnUnit { get; set; } = null!;

    [Column(TypeName = "decimal(10, 2)")]
    public required decimal ReturnRate { get; set; }
    [Column(TypeName = "decimal(10, 3)")]
    public required decimal ReturnQuantity { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal ReturnAmount { get; set; }
    public required string Reason { get; set; }
}