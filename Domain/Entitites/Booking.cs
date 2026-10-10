namespace Domain.Entitites;

[Table("Booking", Schema = "product")]
public class Booking : AuditableEntity<Guid>
{
    public required string BookingNumber { get; set; }
    public string? ReferenceNumber { get; set; }
    public required DateTime BookingDate { get; set; }
    public required int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public required int BranchId { get; set; }
    public Branch? Branch { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Contract booking (চুক্তি): no money is collected at delivery time; the customer is billed per
    /// cycle through the Customer Bill Statement, which only lists contract bookings.
    /// </summary>
    public bool IsContracted { get; set; } = false;
    public ICollection<BookingDetail> BookingDetails { get; set; } = [];
    //public ICollection<Transaction> Transactions { get; set; } = [];
}
