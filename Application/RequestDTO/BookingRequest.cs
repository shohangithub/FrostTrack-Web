namespace Application.RequestDTO;

public record BookingRequest(
        Guid Id,
        string BookingNumber,
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
       decimal BaseRate
  );
