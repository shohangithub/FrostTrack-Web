namespace Domain.Entitites;

[Table("DeliveryDetail", Schema = "product")]
public class DeliveryDetail : AuditableEntity<Guid>
{
    public required Guid DeliveryId { get; set; }
    public Delivery? Delivery { get; set; }
    public required Guid BookingDetailId { get; set; }
    public BookingDetail? BookingDetail { get; set; }

    public required int DeliveryUnitId { get; set; }
    public UnitConversion? DeliveryUnit { get; set; }
    public required float DeliveryQuantity { get; set; }
    public required decimal BaseQuantity { get; set; }

    // Charge Amount for this Delivery Detail

    [Column(TypeName = "decimal(10, 2)")]
    public required decimal ChargeAmount { get; set; } = 0;

    [Column(TypeName = "decimal(5, 2)")]
    public required decimal AdjustmentValue { get; set; } = 0;
}
