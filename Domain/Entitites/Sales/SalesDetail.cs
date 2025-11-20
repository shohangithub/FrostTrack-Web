namespace Domain.Entitites;

[Table("SalesDetails", Schema = "product")]
public class SalesDetail : AuditableEntity<long>
{
    public long SalesId { get; set; }
    public Sales? Sales { get; set; }
    public required int ProductId { get; set; }
    public Product? Product { get; set; }
    public int SalesUnitId { get; set; }
    public UnitConversion? SalesUnit { get; set; }
    public required float SalesQuantity { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal SalesRate { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal SalesAmount { get; set; }


    //public required int BaseUnitId { get; set; }
    //public UnitConversion? BaseUnit { get; set; }
    //public required float BaseQuantity { get; set; }
    //[Column(TypeName = "decimal(10, 2)")]
    //public required decimal SalesRate { get; set; }
}
