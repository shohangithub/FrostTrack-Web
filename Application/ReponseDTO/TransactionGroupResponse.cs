namespace Application.ReponseDTO;

public record TransactionGroupResponse(
    Guid Id,
    string Code,
    string Name,
    int SortOrder,
    string? Description,
    bool IsActive,
    string Status,
    string? AccountType
);

public record TransactionGroupListResponse(
    Guid Id,
    string Code,
    string Name,
    int SortOrder,
    int HeadCount,
    string Status,
    string? AccountType = null
);
