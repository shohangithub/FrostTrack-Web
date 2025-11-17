namespace Domain.Entitites;

[Table("ProductDeliveryDetails", Schema = "product")]
public class ProductDeliveryDetail
{
    [Key]
    public long Id { get; set; }
    public required long ProductDeliveryId { get; set; }
    public ProductDelivery ProductDelivery { get; set; }
    public required int ProductId { get; set; }
    public Product Product { get; set; }
    public required int DeliveryUnitId { get; set; }
    public BaseUnit DeliveryUnit { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public required decimal DeliveryQuantity { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal BookingRate { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal DeliveryAmount { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedTime { get; set; } = DateTime.Now;
}
