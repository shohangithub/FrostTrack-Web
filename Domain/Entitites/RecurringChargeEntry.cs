namespace Domain.Entitites;

/// <summary>
/// Charge ledger: one row per charge on a booking (rent per billing month, booking/delivery labour,
/// delivery adjustment). Append-only — changes are posted as CORRECTION rows, rows are never edited.
/// A booking's charges as of a date = sum of Amount where EntryDate &lt;= that date (ChargeType != LEGACY).
/// </summary>
[Table("RecurringChargeEntries", Schema = "product")]
public class RecurringChargeEntry : BaseEntity<Guid>
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    /// <summary>Booking line; null for delivery adjustments (or after the line was removed).</summary>
    public Guid? BookingDetailId { get; set; }
    public BookingDetail? BookingDetail { get; set; }

    /// <summary>RENT_CYCLE | BOOKING_LABOUR | DELIVERY_LABOUR | DELIVERY_ADJUSTMENT | LEGACY (see ChargeTypes).</summary>
    [MaxLength(30)]
    public string ChargeType { get; set; } = ChargeTypes.RentCycle;

    /// <summary>Billing month number (1 = first month) for RENT_CYCLE entries.</summary>
    public int? CycleNo { get; set; }

    /// <summary>Delivery the charge comes from (labour/adjustment). No FK: delivery history is kept.</summary>
    public Guid? DeliveryId { get; set; }

    /// <summary>Date the charge applies from (month start, booking date or delivery date; posting date for corrections).</summary>
    [Column(TypeName = "datetime2")]
    public DateTime EntryDate { get; set; }

    public int? CreatedById { get; set; }

    /// <summary>Null for Source = INITIAL; populated for Source = RUN.</summary>
    public Guid? RecurringChargeRunId { get; set; }
    public RecurringChargeRun? RecurringChargeRun { get; set; }

    /// <summary>INITIAL (created at booking time) | RUN (created by a recurring-charge run).</summary>
    [MaxLength(20)]
    public string Source { get; set; } = RecurringChargeSources.Posted;

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
