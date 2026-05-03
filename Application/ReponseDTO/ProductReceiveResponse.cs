using Domain.Entitites;

namespace Application.ReponseDTO;

public record ProductReceiveResponse(
        Guid Id,
        string BookingNumber,
        DateTime BookingDate,
        int CustomerId,
        Customer Customer,
        int BranchId,
        string? Notes,
  bool IsDeleted,
  bool IsArchived,
  DateTime? DeletedAt,
  DateTime? ArchivedAt,
        ICollection<ProductReceiveDetailResponse> BookingDetails
    );

public record ProductReceiveDetailResponse(
       Guid Id,
       Guid BookingDetailId,
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
       Guid Id,
       Guid BookingDetailId,
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
        Guid Id,
        string BookingNumber,
        DateTime BookingDate,
        int CustomerId,
        Customer Customer,
        int BranchId,
        Branch Branch,
        string? Notes,
        bool IsDeleted,
        bool IsArchived,
        DateTime? DeletedAt,
        DateTime? ArchivedAt,
        IEnumerable<ProductReceiveDetailListResponse> BookingDetails
    );
