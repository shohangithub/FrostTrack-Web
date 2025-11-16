namespace Application.RequestDTO;

public record PurchaseRequest(
        long Id,
        string InvoiceNumber,
        DateTime InvoiceDate,
        int SupplierId,
        decimal Subtotal,
        float VatPercent,
        decimal VatAmount,
        float DiscountPercent,
        decimal DiscountAmount,
        decimal OtherCost,
        decimal InvoiceAmount,
        decimal PaidAmount,
        int BranchId,
        ICollection<PurchaseDetailRequest> PurchaseDetails
   );

public record PurchaseDetailRequest(
       long Id,
       int PurchaseId,
       int ProductId,
       int PurchaseUnitId,
       decimal PurchaseRate,
       float PurchaseQuantity,
       decimal PurchaseAmount
  );