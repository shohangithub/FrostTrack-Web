namespace Application.RequestDTO;

public record CustomerRequest(
    int Id,
    string CustomerName,
    string CustomerCode,
    string? CustomerMobile,
    string? CustomerEmail,
    string? OfficePhone,
    string? Address,
    string? ImageUrl,
    decimal CreditLimit,
    decimal OpeningBalance,
    bool IsActive,
    int? BranchId
    );