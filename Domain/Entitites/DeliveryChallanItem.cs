namespace Domain.Entitites;

[Table("DeliveryChallanItem", Schema = "product")]
public class DeliveryChallanItem : BaseEntity<Guid>
{
    public required Guid DeliveryChallanId { get; set; }
    public DeliveryChallan? DeliveryChallan { get; set; }

    public required Guid DeliveryId { get; set; }
    public Delivery? Delivery { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
