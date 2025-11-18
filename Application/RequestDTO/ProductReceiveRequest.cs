namespace Application.RequestDTO;

public record ProductReceiveRequest(
        long Id,
        string BookingNumber,
        DateTime BookingDate,
        int CustomerId,
        int BranchId,
        string? Notes,
        ICollection<ProductReceiveDetailRequest> BookingDetails
   );

public record ProductReceiveDetailRequest(
       long Id,
       long BookingDetailId,
       int ProductId,
       int BookingUnitId,
       float BookingQuantity,
       decimal BookingRate,
       decimal BaseQuantity,
       decimal BaseRate
  );
