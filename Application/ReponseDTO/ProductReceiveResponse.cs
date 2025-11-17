using Domain.Entitites;

namespace Application.ReponseDTO;

public record ProductReceiveResponse(
        long Id,
        string ReceiveNumber,
        DateTime ReceiveDate,
        int CustomerId,
        Customer Customer,
        decimal Subtotal,
        decimal VatAmount,
        float DiscountPercent,
        decimal DiscountAmount,
        decimal OtherCost,
        decimal TotalAmount,
        decimal PaidAmount,
        int BranchId,
        string? Notes,
        ICollection<ProductReceiveDetailResponse> ProductReceiveDetails
    );

public record ProductReceiveDetailResponse(
       long Id,
       long ProductReceiveId,
       int ProductId,
       ProductResponse? Product,
       int ReceiveUnitId,
       UnitConversionResponse? ReceiveUnit,
       decimal BookingRate,
       float ReceiveQuantity,
       decimal ReceiveAmount
  );

public record ProductReceiveDetailListResponse(
       long Id,
       long ProductReceiveId,
       int ProductId,
       string ProductName,
       int ReceiveUnitId,
       string UnitName,
       decimal BookingRate,
       float ReceiveQuantity,
       decimal ReceiveAmount
  );

public record ProductReceiveListResponse(
        long Id,
        string ReceiveNumber,
        DateTime ReceiveDate,
        int CustomerId,
        Customer Customer,
        decimal Subtotal,
        float VatPercent,
        decimal VatAmount,
        float DiscountPercent,
        decimal DiscountAmount,
        decimal OtherCost,
        decimal TotalAmount,
        decimal PaidAmount,
        int BranchId,
        Branch Branch,
        string? Notes,
        IEnumerable<ProductReceiveDetailListResponse> ProductReceiveDetails
    );
