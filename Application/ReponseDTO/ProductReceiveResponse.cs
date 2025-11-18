using Domain.Entitites;

namespace Application.ReponseDTO;

public record ProductReceiveResponse(
        long Id,
        string BookingNumber,
        DateTime BookingDate,
        int CustomerId,
        Customer Customer,
        int BranchId,
        string? Notes,
        ICollection<ProductReceiveDetailResponse> BookingDetails
    );

public record ProductReceiveDetailResponse(
       long Id,
       long BookingDetailId,
       int ProductId,
       ProductResponse? Product,
       int BookingUnitId,
       UnitConversionResponse? BookingUnit,
       float BookingQuantity,
       decimal BookingRate,
       decimal BaseQuantity,
       decimal BaseRate
  );

public record ProductReceiveDetailListResponse(
       long Id,
       long BookingDetailId,
       int ProductId,
       string ProductName,
       int BookingUnitId,
       string UnitName,
       float BookingQuantity,
       decimal BookingRate,
       decimal BaseQuantity,
       decimal BaseRate
  );

public record ProductReceiveListResponse(
        long Id,
        string BookingNumber,
        DateTime BookingDate,
        int CustomerId,
        Customer Customer,
        int BranchId,
        Branch Branch,
        string? Notes,
        IEnumerable<ProductReceiveDetailListResponse> BookingDetails
    );
