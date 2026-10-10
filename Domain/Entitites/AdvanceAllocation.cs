namespace Domain.Entitites;

/// <summary>
/// Part of a customer advance (a CUSTOMER_PAYMENT transaction without a booking) applied to a booking's due.
/// No cash moves: the money was recorded when the advance was received; this only moves it onto a booking.
/// </summary>
[Table("AdvanceAllocations", Schema = "finance")]
public class AdvanceAllocation : AuditableEntity<Guid>
{
    /// <summary>The advance payment the amount is taken from.</summary>
    public required Guid AdvanceTransactionId { get; set; }
    public Transaction? AdvanceTransaction { get; set; }

    public required Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public required int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public required decimal Amount { get; set; }

    public required DateTime AllocationDate { get; set; }

    /// <summary>Receipt number of the booking payment this allocation was made with.</summary>
    [MaxLength(50)]
    public string? ReceiptCode { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
