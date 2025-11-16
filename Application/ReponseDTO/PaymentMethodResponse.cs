namespace Application.ReponseDTO;

public record PaymentMethodResponse(
    int Id,
    string MethodName,
    string Code,
    string? Description,
    string Category,
    bool RequiresBankAccount,
    bool RequiresCheckDetails,
    bool RequiresOnlineDetails,
    bool RequiresMobileWalletDetails,
    bool RequiresCardDetails,
    bool IsActive,
    int SortOrder,
    string? IconClass,
    int? BranchId,
    string Status
);

public record PaymentMethodListResponse(
    int Id,
    string MethodName,
    string Code,
    string? Description,
    string Category,
    bool RequiresBankAccount,
    bool RequiresCheckDetails,
    bool RequiresOnlineDetails,
    bool RequiresMobileWalletDetails,
    bool RequiresCardDetails,
    bool IsActive,
    int SortOrder,
    string? IconClass,
    int? BranchId,
    string Status
);