namespace Domain.Entitites;

/// <summary>
/// Permanent ledger record for each recurring-charge cycle charged to a booking detail.
/// Created at booking time (Source = INITIAL) and on each recurring-charge run (Source = RUN).
/// Never updated — append-only audit trail.
/// </summary>
[Table("RecurringChargeEntries", Schema = "product")]
public class RecurringChargeEntry : BaseEntity<Guid>
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public Guid BookingDetailId { get; set; }
    public BookingDetail? BookingDetail { get; set; }

    /// <summary>Null for Source = INITIAL; populated for Source = RUN.</summary>
    public Guid? RecurringChargeRunId { get; set; }
    public RecurringChargeRun? RecurringChargeRun { get; set; }

    /// <summary>INITIAL (created at booking time) | RUN (created by a recurring-charge run).</summary>
    [MaxLength(20)]
    public string Source { get; set; } = RecurringChargeSources.Initial;

    [Column(TypeName = "datetime2")]
    public DateTime BillPeriodFrom { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime BillPeriodTo { get; set; }

    /// <summary>HOURLY | DAILY | WEEKLY | MONTHLY | YEARLY</summary>
    [MaxLength(20)]
    public string BillType { get; set; } = BillTypes.Monthly;

    public int Cycles { get; set; } = 1;

    public float Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Rate { get; set; }

    /// <summary>Cycles × Quantity × Rate</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
