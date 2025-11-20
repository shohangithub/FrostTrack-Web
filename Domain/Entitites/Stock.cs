namespace Domain.Entitites;

[Table("Stocks", Schema = "product")]
public class Stock : AuditableEntity<long>
{
    public required int ProductId { get; set; }
    public Product? Product { get; set; }
    public required int UnitId { get; set; }
    public UnitConversion? UnitConversion { get; set; }
    public required float StockQuantity { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal LastPurchaseRate { get; set; }
    public required int BranchId { get; set; }
}

