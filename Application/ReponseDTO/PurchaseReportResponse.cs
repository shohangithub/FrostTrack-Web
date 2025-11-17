namespace Application.ReponseDTO;

public record PurchaseInvoiceReportResponse(
        long Id,
        string InvoiceNumber,
        DateTime InvoiceDate,
        decimal Subtotal,
        float VatPercent,
        decimal VatAmount,
        float DiscountPercent,
        decimal DiscountAmount,
        decimal OtherCost,
        decimal InvoiceAmount,
        decimal PaidAmount,
        int BranchId,
        SupplierReportResponse Supplier,
        List<PurchaseDetailReportResponse> PurchaseDetails
    );

public record SupplierReportResponse(
    string SupplierName,
    string SupplierCode,
    string SupplierBarcode,
    string? SupplierMobile,
    string? Address,
    decimal CreditLimit,
    decimal OpeningBalance,
    decimal PreviousDue);

public record PurchaseDetailReportResponse(
       long Id,
       decimal PurchaseRate,
       float PurchaseQuantity,
       decimal PurchaseAmount,
       ProductReportResponse? Product,
       UnitConversionReportResponse? PurchaseUnit
  );


public record ProductReportResponse(
    int Id,
    string ProductName,
    string ProductCode,
    int CategoryId,
    int? DefaultUnitId,
    string? ImageUrl,
    decimal? BookingRate,
    bool IsActive,
    string Status
    );

public record UnitConversionReportResponse(
    int Id,
    string UnitName,
    int BaseUnitId,
    float ConversionValue,
    string Description,
    bool IsActive,
    string Status
);