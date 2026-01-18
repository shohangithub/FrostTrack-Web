namespace Application.RequestDTO;

public record DeliveryChallanRequest
{
    public Guid Id { get; init; }
    public required string ChallanNumber { get; init; }
    public required DateTime ChallanDate { get; init; }
    public required string VehicleNumber { get; init; }
    public string? DriverName { get; init; }
    public string? DriverContact { get; init; }
    public string? VehicleType { get; init; }
    public string? TransportCompany { get; init; }
    public string? Destination { get; init; }
    public required int BranchId { get; init; }
    public string? Remarks { get; init; }
    public string Status { get; init; } = "Pending";
    public DateTime? DispatchTime { get; init; }
    public DateTime? DeliveryTime { get; init; }
    public required List<Guid> DeliveryIds { get; init; }
}
