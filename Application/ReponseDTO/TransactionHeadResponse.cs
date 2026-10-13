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
    string Status,
    Guid? TransactionGroupId,
    string? AccountCategory
);

public record TransactionHeadListResponse(
    Guid Id,
    string Code,
    string Name,
    string Type,
    string DisplayType,
    bool IsSystem,
    string Status,
    string? TransactionGroupName = null,
    string? AccountCategory = null
);

public record TransactionHeadLookup(
    Guid Id,
    string Name,
    string Type,
    string? DisplayType = null
);