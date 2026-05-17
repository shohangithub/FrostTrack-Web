namespace Domain.Entitites;

/// <summary>
/// Permanent ledger record for each payment received against a booking.
/// Created alongside a Transaction record whenever a bill collection is processed.
/// </summary>
[Table("BookingPayments", Schema = "product")]
public class BookingPayment : BaseEntity<Guid>
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>The Transaction that triggered this payment record.</summary>
    public Guid? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public Guid? DeliveryId { get; set; }
    public Delivery? Delivery { get; set; }

    [MaxLength(30)]
    public string TransactionCode { get; set; } = string.Empty;

    [Column(TypeName = "datetime2")]
    public DateTime TransactionDate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal AdjustmentValue { get; set; } = 0;

    /// <summary>Amount - DiscountAmount + AdjustmentValue</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal NetAmount { get; set; }

    /// <summary>CASH | BANK_TRANSFER | CHEQUE | CARD | MOBILE_BANKING</summary>
    [MaxLength(30)]
    public string PaymentMethod { get; set; } = PaymentMethods.CASH;

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
