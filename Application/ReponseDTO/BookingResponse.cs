using Domain.Entitites;

namespace Application.ReponseDTO;

public record BookingResponse(
        Guid Id,
        string BookingNumber,
        DateTime BookingDate,
        int CustomerId,
        Customer Customer,
        int BranchId,
        Branch Branch,
        string? Notes,
        ICollection<BookingDetailResponse> BookingDetails
    );

public record BookingDetailResponse(
       Guid Id,
       Guid BookingId,
       int ProductId,
       ProductResponse? Product,
       int BookingUnitId,
       UnitConversionResponse? BookingUnit,
       float BookingQuantity,
       string BillType,
       decimal BookingRate,
       decimal BaseQuantity,
       decimal BaseRate,
       DateTime LastDeliveryDate
  );

public record BookingDetailListResponse(
       Guid Id,
       Guid BookingId,
       int ProductId,
       string ProductName,
       int BookingUnitId,
       string UnitName,
       float BookingQuantity,
       string BillType,
       decimal BookingRate,
       decimal BaseQuantity,
       decimal BaseRate,
       DateTime LastDeliveryDate
  );

public record BookingListResponse(
        Guid Id,
        string BookingNumber,
        DateTime BookingDate,
        int CustomerId,
        Customer Customer,
        int BranchId,
        Branch Branch,
        string? Notes,
        IEnumerable<BookingDetailListResponse> BookingDetails
    );
