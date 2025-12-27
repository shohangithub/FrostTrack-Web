namespace Domain.Entitites;

[Table("Booking", Schema = "product")]
public class Booking : AuditableEntity<Guid>
{
    public required string BookingNumber { get; set; }
    public required DateTime BookingDate { get; set; }
    public required int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public required int BranchId { get; set; }
    public Branch? Branch { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }
    public int? ArchivedById { get; set; }
    public ICollection<BookingDetail> BookingDetails { get; set; } = [];
    //public ICollection<Transaction> Transactions { get; set; } = [];
}
