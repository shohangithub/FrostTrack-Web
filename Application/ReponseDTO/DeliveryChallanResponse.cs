namespace Application.ReponseDTO;

public record DeliveryChallanResponse(
    Guid Id,
    string ChallanNumber,
    DateTime ChallanDate,
    string VehicleNumber,
    string? DriverName,
    string? DriverContact,
    string? VehicleType,
    string? TransportCompany,
    string? Destination,
    int BranchId,
    string? Remarks,
    string Status,
    DateTime? DispatchTime,
    DateTime? DeliveryTime,
    List<DeliveryChallanItemResponse> ChallanItems
);

public record DeliveryChallanItemResponse(
    Guid Id,
    Guid DeliveryChallanId,
    Guid DeliveryId,
    string DeliveryNumber,
    DateTime DeliveryDate,
    string BookingNumber,
    string CustomerName,
    decimal ChargeAmount,
    string? Notes
);

public record DeliveryChallanListResponse(
    Guid Id,
    string ChallanNumber,
    DateTime ChallanDate,
    string VehicleNumber,
    string? DriverName,
    string? Destination,
    string Status,
    int TotalDeliveries,
    decimal TotalAmount,
    DateTime? DispatchTime,
    DateTime? DeliveryTime
);
