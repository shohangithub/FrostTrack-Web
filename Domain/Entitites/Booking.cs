namespace Domain.Entitites;

[Table("Bookings", Schema = "product")]
public class Booking : AuditableEntity<long>
{
    public required string BookingNumber { get; set; }
    public required DateTime BookingDate { get; set; }
    public required int CustomerId { get; set; }
    public Customer Customer { get; set; }
    // [Column(TypeName = "decimal(10, 2)")]
    // public required decimal Subtotal { get; set; }
    // public required float VatPercent { get; set; }
    // [Column(TypeName = "decimal(10, 2)")]
    // public required decimal VatAmount { get; set; }
    // public required float DiscountPercent { get; set; }
    // [Column(TypeName = "decimal(10, 2)")]
    // public required decimal DiscountAmount { get; set; }
    // [Column(TypeName = "decimal(10, 2)")]
    // public required decimal OtherCost { get; set; }
    // [Column(TypeName = "decimal(10, 2)")]
    // public required decimal TotalAmount { get; set; }
    // [Column(TypeName = "decimal(10, 2)")]
    // public required decimal PaidAmount { get; set; }
    public required int BranchId { get; set; }
    public Branch Branch { get; set; }
    public string? Notes { get; set; }
    public ICollection<BookingDetail> BookingDetails { get; set; } = [];
    //public ICollection<Transaction> Transactions { get; set; } = [];
}
