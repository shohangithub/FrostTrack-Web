namespace Domain.Entitites;

[Table("Delivery", Schema = "product")]
public class Delivery : AuditableEntity<Guid>
{
    public required string DeliveryNumber { get; set; }
    public required DateTime DeliveryDate { get; set; }
    public required Guid BookingId { get; set; }
    public Booking? Booking { get; set; }
    public required int BranchId { get; set; }
    public Branch? Branch { get; set; }
    public string? Notes { get; set; }



    [Column(TypeName = "decimal(10, 2)")]
    public required decimal ChargeAmount { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public required decimal AdjustmentValue { get; set; }


    // [Column(TypeName = "decimal(10, 2)")]
    // public required decimal DiscountAmount { get; set; } = 0;

    // [Column(TypeName = "decimal(10, 2)")]
    // public required decimal PaidAmount { get; set; } = 0;

    // Soft delete and archive
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedById { get; set; }

    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }
    public int? ArchivedById { get; set; }

    public ICollection<DeliveryDetail> DeliveryDetails { get; set; } = [];
}
