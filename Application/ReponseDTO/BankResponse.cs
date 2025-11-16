namespace Application.ReponseDTO;

public record BankResponse(
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
    string Status
    );

public record BankListResponse(
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
    string Status
    );