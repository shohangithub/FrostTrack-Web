namespace Domain.Entitites;

/// <summary>
/// Permanent ledger record for each delivery charge line.
/// One row per DeliveryDetail — tracks what was delivered, at what quantity and rate.
/// </summary>
[Table("BookingCharges", Schema = "product")]
public class BookingCharge : BaseEntity<Guid>
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public Guid BookingDetailId { get; set; }
    public BookingDetail? BookingDetail { get; set; }

    public Guid DeliveryId { get; set; }
    public Delivery? Delivery { get; set; }

    [MaxLength(30)]
    public string DeliveryNumber { get; set; } = string.Empty;

    [Column(TypeName = "datetime2")]
    public DateTime DeliveryDate { get; set; }

    public float Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Rate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ChargeAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal LabourCharge { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal AdjustmentValue { get; set; } = 0;

    /// <summary>ChargeAmount + LabourCharge + AdjustmentValue</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal NetAmount { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
