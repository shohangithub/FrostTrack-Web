namespace Application.ReponseDTO;

public record SalesResponse(
        long Id,
        string InvoiceNumber,
        DateTime InvoiceDate,
        string SalesType,
        int CustomerId,
        Customer Customer,
        decimal Subtotal,
        float VatPercent,
        decimal VatAmount,
        float DiscountPercent,
        decimal DiscountAmount,
        decimal OtherCost,
        decimal InvoiceAmount,
        decimal PaidAmount,
        int BranchId,
        ICollection<SalesDetailResponse> SalesDetails
    );
public record SalesDetailResponse(
       long Id,
       long SalesId,
       int ProductId,
       ProductResponse? Product,
       int SalesUnitId,
       UnitConversionResponse? SalesUnit,
       decimal SalesRate,
       float SalesQuantity,
       decimal SalesAmount
  );

public record SalesDetailListResponse(
       long Id,
       long SalesId,
       int ProductId,
       string ProductName,
       int SalesUnitId,
       string UnitName,
       decimal SalesRate,
       float SalesQuantity,
       decimal SalesAmount
  );
public record SalesListResponse(
        long Id,
        string InvoiceNumber,
        DateTime InvoiceDate,
        string SalesType,
        int CustomerId,
        Customer Customer,
        decimal Subtotal,
        float VatPercent,
        decimal VatAmount,
        float DiscountPercent,
        decimal DiscountAmount,
        decimal OtherCost,
        decimal InvoiceAmount,
        decimal PaidAmount,
        int BranchId,
        Branch Branch,
        IEnumerable<SalesDetailListResponse> SalesDetails
    );