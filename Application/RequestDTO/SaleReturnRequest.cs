namespace Application.RequestDTO;

public record SaleReturnRequest(
        long Id,
        string ReturnNumber,
        DateTime ReturnDate,
        long SalesId,
        int CustomerId,
        decimal Subtotal,
        float VatPercent,
        decimal VatAmount,
        float DiscountPercent,
        decimal DiscountAmount,
        decimal OtherCost,
        decimal ReturnAmount,
        string Reason,
        int BranchId,
        ICollection<SaleReturnDetailRequest> SaleReturnDetails
   );

public record SaleReturnDetailRequest(
       long Id,
       long SaleReturnId,
       int ProductId,
       int ReturnUnitId,
       decimal ReturnRate,
       float ReturnQuantity,
       decimal ReturnAmount,
       string Reason
  );