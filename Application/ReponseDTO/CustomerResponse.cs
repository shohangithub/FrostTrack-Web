namespace Application.ReponseDTO;

public record CustomerResponse(
    int Id,
    string CustomerName,
    string CustomerCode,
    string? CustomerBarcode,
    string? CustomerMobile,
    string? CustomerEmail,
    string? OfficePhone,
    string? Address,
    string? ImageUrl,
    decimal CreditLimit,
    decimal OpeningBalance,
    bool IsActive,
    string Status
    );
public record CustomerListResponse(
    int Id,
    string CustomerName,
    string CustomerCode,
    string? CustomerBarcode,
    string? CustomerMobile,
    string? CustomerEmail,
    string? OfficePhone,
    string? Address,
    string? ImageUrl,
    decimal CreditLimit,
    decimal PreviousDue,
    decimal OpeningBalance,
    bool IsSystemDefault,
    string Status
    );
