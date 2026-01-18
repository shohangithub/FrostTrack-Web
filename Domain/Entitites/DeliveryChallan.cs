namespace Domain.Entitites;

[Table("DeliveryChallan", Schema = "product")]
public class DeliveryChallan : AuditableEntity<Guid>
{
    [MaxLength(50)]
    public required string ChallanNumber { get; set; }

    public required DateTime ChallanDate { get; set; }

    [MaxLength(50)]
    public required string VehicleNumber { get; set; }

    [MaxLength(100)]
    public string? DriverName { get; set; }

    [MaxLength(20)]
    public string? DriverContact { get; set; }

    [MaxLength(100)]
    public string? VehicleType { get; set; }

    [MaxLength(500)]
    public string? TransportCompany { get; set; }

    [MaxLength(100)]
    public string? Destination { get; set; }

    public required int BranchId { get; set; }
    public Branch? Branch { get; set; }

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, In Transit, Delivered, Cancelled

    public DateTime? DispatchTime { get; set; }
    public DateTime? DeliveryTime { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedById { get; set; }

    public ICollection<DeliveryChallanItem> ChallanItems { get; set; } = [];
}
