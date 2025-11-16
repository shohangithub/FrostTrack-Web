namespace Application.ReponseDTO;

public record SupplierResponse(
    int Id,
    string SupplierName,
    string SupplierCode,
    string? SupplierBarcode,
    string? SupplierMobile,
    string? SupplierEmail,
    string? OfficePhone,
    string? Address,
    string? ImageUrl,
    decimal CreditLimit,
    decimal OpeningBalance,
    bool IsActive,
    string Status
    );
public record SupplierListResponse(
    int Id,
    string SupplierName,
    string SupplierCode,
    string? SupplierBarcode,
    string? SupplierMobile,
    string? SupplierEmail,
    string? OfficePhone,
    string? Address,
    string? ImageUrl,
    decimal CreditLimit,
    decimal OpeningBalance,
    decimal PreviousDue,
    bool IsSystemDefault,
    string Status
    );
