namespace Domain.Entitites;

[Table("ProductDeliveryDetails", Schema = "product")]
public class ProductDeliveryDetail : AuditableEntity<long>
{
    public long ProductDeliveryId { get; set; }
    public ProductDelivery ProductDelivery { get; set; }
    public required int ProductId { get; set; }
    public Product Product { get; set; }
    public int DeliveryUnitId { get; set; }
    public UnitConversion DeliveryUnit { get; set; }
    public required float DeliveryQuantity { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal DeliveryRate { get; set; }

    // Base Conversion from Delivery Unit to Base Unit
    public required decimal BaseQuantity { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal BaseRate { get; set; }
}
