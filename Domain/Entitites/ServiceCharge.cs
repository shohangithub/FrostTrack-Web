namespace Domain.Entitites;

[Table("ServiceCharges")]
public class ServiceCharge : AuditableEntity<Guid>
{
    public Guid ServiceChargeId { get; set; }
    public required string ServiceChargeCode { get; set; }
    public Booking Booking { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public required decimal Amount { get; set; }
    public string? Note { get; set; } = null;
}