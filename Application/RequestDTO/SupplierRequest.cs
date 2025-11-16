namespace Application.RequestDTO;

public record SupplierRequest(
    int Id,
    string SupplierName,
    string SupplierCode,
    string? SupplierMobile,
    string? SupplierEmail,
    string? OfficePhone,
    string? Address,
    string? ImageUrl,
    decimal CreditLimit,
    decimal OpeningBalance,
    bool IsActive,
    int? BranchId
    );