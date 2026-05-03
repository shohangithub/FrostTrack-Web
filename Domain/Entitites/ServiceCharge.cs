namespace Domain.Entitites;

[Table("ServiceCharges")]
public class ServiceCharge : AuditableEntity<Guid>
{
    public required string ServiceChargeCode { get; set; }
    public required Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public required decimal Amount { get; set; }
    public string? Note { get; set; } = null;
}