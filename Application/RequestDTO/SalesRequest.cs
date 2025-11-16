namespace Application.RequestDTO;

public record SalesRequest(
        long Id,
        string InvoiceNumber,
        DateTime InvoiceDate,
        string SalesType,
        int CustomerId,
        decimal Subtotal,
        float VatPercent,
        decimal VatAmount,
        float DiscountPercent,
        decimal DiscountAmount,
        decimal OtherCost,
        decimal InvoiceAmount,
        decimal PaidAmount,
        int BranchId,
        ICollection<SalesDetailRequest> SalesDetails
   );

public record SalesDetailRequest(
       long Id,
       int SalesId,
       int ProductId,
       int SalesUnitId,
       decimal SalesRate,
       float SalesQuantity,
       decimal SalesAmount
  );