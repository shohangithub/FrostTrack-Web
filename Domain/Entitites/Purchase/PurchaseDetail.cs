namespace Domain.Entitites;

[Table("PurchaseDetails", Schema = "product")]
public class PurchaseDetail : AuditableEntity<long>
{
    public long PurchaseId { get; set; }
    public Purchase Purchase { get; set; }
    public required int ProductId { get; set; }
    public Product Product { get; set; }
    public int PurchaseUnitId { get; set; }
    public UnitConversion PurchaseUnit { get; set; }
    public required float PurchaseQuantity { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal PurchaseRate { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal PurchaseAmount { get; set; }


    //public required int BaseUnitId { get; set; }
    //public UnitConversion? BaseUnit { get; set; }
    //public required float BaseQuantity { get; set; }
    //[Column(TypeName = "decimal(10, 2)")]
    //public required decimal PurchaseRate { get; set; }
}
