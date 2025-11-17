namespace Domain.Entitites;

[Table("ProductReceiveDetails", Schema = "product")]
public class ProductReceiveDetail : AuditableEntity<long>
{
    public long ProductReceiveId { get; set; }
    public ProductReceive ProductReceive { get; set; }
    public required int ProductId { get; set; }
    public Product Product { get; set; }
    public int ReceiveUnitId { get; set; }
    public UnitConversion ReceiveUnit { get; set; }
    public required float ReceiveQuantity { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal BookingRate { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal ReceiveAmount { get; set; }
}
