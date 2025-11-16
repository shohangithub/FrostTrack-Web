namespace Application.RequestDTO;

public record PaymentMethodRequest(
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
    int? BranchId
);