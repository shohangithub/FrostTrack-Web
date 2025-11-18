namespace Application.RequestDTO;

public record ProductReceiveRequest(
        Guid Id,
        string BookingNumber,
        DateTime BookingDate,
        int CustomerId,
        int BranchId,
        string? Notes,
        ICollection<ProductReceiveDetailRequest> BookingDetails
   );

public record ProductReceiveDetailRequest(
       Guid Id,
       Guid BookingDetailId,
       int ProductId,
       int BookingUnitId,
       float BookingQuantity,
       decimal BookingRate,
       decimal BaseQuantity,
       decimal BaseRate
  );
