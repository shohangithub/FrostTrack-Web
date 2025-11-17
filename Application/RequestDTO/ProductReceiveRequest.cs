namespace Application.RequestDTO;

public record ProductReceiveRequest(
        long Id,
        string ReceiveNumber,
        DateTime ReceiveDate,
        int CustomerId,
        decimal Subtotal,
        float VatPercent,
        decimal VatAmount,
        float DiscountPercent,
        decimal DiscountAmount,
        decimal OtherCost,
        decimal TotalAmount,
        decimal PaidAmount,
        int BranchId,
        string? Notes,
        ICollection<ProductReceiveDetailRequest> ProductReceiveDetails
   );

public record ProductReceiveDetailRequest(
       long Id,
       int ProductReceiveId,
       int ProductId,
       int ReceiveUnitId,
       decimal BookingRate,
       float ReceiveQuantity,
       decimal ReceiveAmount
  );
