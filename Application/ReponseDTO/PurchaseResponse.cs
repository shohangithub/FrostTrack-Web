using Domain.Entitites;

namespace Application.ReponseDTO;

public record PurchaseResponse(
        long Id,
        string InvoiceNumber,
        DateTime InvoiceDate,
        int SupplierId,
        Supplier Supplier,
        decimal Subtotal,
        float VatPercent,
        decimal VatAmount,
        float DiscountPercent,
        decimal DiscountAmount,
        decimal OtherCost,
        decimal InvoiceAmount,
        decimal PaidAmount,
        int BranchId,
        ICollection<PurchaseDetailResponse> PurchaseDetails
    );
public record PurchaseDetailResponse(
       long Id,
       long PurchaseId,
       int ProductId,
       ProductResponse? Product,
       int PurchaseUnitId,
       UnitConversionResponse? PurchaseUnit,
       decimal PurchaseRate,
       float PurchaseQuantity,
       decimal PurchaseAmount
  );

public record PurchaseDetailListResponse(
       long Id,
       long PurchaseId,
       int ProductId,
       string ProductName,
       int PurchaseUnitId,
       string UnitName,
       decimal PurchaseRate,
       float PurchaseQuantity,
       decimal PurchaseAmount
  );
public record PurchaseListResponse(
        long Id,
        string InvoiceNumber,
        DateTime InvoiceDate,
        int SupplierId,
        Supplier Supplier,
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
        IEnumerable<PurchaseDetailListResponse> PurchaseDetails
    );