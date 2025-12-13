namespace Application.ReponseDTO;

public record TransactionHeadResponse(
    Guid Id,
    string Code,
    string Name,
    string Type,
    string DisplayType,
    int SortOrder,
    string? Description,
    bool IsActive,
    bool IsSystem,
    string? ColorCode,
    string? IconClass,
    string Status
);

public record TransactionHeadListResponse(
    Guid Id,
    string Code,
    string Name,
    string Type,
    string DisplayType,
    bool IsSystem,
    string Status
);
