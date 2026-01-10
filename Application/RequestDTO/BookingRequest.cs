namespace Application.RequestDTO;

public record BookingRequest(
        Guid Id,
        string BookingNumber,
        string? ReferenceNumber,
        DateTime BookingDate,
        int CustomerId,
        int BranchId,
        string? Notes,
        ICollection<BookingDetailRequest> BookingDetails
   );

public record BookingDetailRequest(
       Guid Id,
       Guid BookingId,
       int ProductId,
       int BookingUnitId,
       float BookingQuantity,
       decimal BookingRate,
       decimal BaseQuantity,
       decimal BaseRate,
       decimal LabourCharge,
       DateTime? LastDeliveryDate
  );
