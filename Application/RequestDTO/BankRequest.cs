namespace Application.RequestDTO;

public record BankRequest(
    int Id,
    string BankName,
    string BankCode,
    string? BankBranch,
    string? AccountNumber,
    string? AccountTitle,
    string? SwiftCode,
    string? RoutingNumber,
    string? IBANNumber,
    string? ContactPerson,
    string? ContactPhone,
    string? ContactEmail,
    string? Address,
    decimal OpeningBalance,
    decimal CurrentBalance,
    string? Description,
    bool IsMainAccount,
    bool IsActive,
    int? BranchId
    );