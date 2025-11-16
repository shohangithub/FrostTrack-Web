namespace Application.ReponseDTO;

public record SaleReturnResponse(
        long Id,
        string ReturnNumber,
        DateTime ReturnDate,
        long SalesId,
        SalesResponse? Sales,
        int CustomerId,
        Customer Customer,
        decimal Subtotal,
        float VatPercent,
        decimal VatAmount,
        float DiscountPercent,
        decimal DiscountAmount,
        decimal OtherCost,
        decimal ReturnAmount,
        string Reason,
        int BranchId,
        Branch Branch,
        ICollection<SaleReturnDetailResponse> SaleReturnDetails
    );

public record SaleReturnDetailResponse(
       long Id,
       long SaleReturnId,
       int ProductId,
       ProductResponse? Product,
       int ReturnUnitId,
       UnitConversionResponse? ReturnUnit,
       decimal ReturnRate,
       float ReturnQuantity,
       decimal ReturnAmount,
       string Reason
  );

public record SaleReturnDetailListResponse(
       long Id,
       long SaleReturnId,
       int ProductId,
       string ProductName,
       int ReturnUnitId,
       string UnitName,
       decimal ReturnRate,
       float ReturnQuantity,
       decimal ReturnAmount,
       string Reason
  );

public record SaleReturnListResponse(
        long Id,
        string ReturnNumber,
        DateTime ReturnDate,
        long SalesId,
        string SalesInvoiceNumber,
        int CustomerId,
        Customer Customer,
        decimal Subtotal,
        float VatPercent,
        decimal VatAmount,
        float DiscountPercent,
        decimal DiscountAmount,
        decimal OtherCost,
        decimal ReturnAmount,
        string Reason,
        int BranchId,
        Branch Branch,
        IEnumerable<SaleReturnDetailListResponse> SaleReturnDetails
    );