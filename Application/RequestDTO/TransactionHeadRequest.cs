namespace Application.RequestDTO;

public record TransactionHeadRequest(
    string Code,
    string Name,
    string Type,
    string? DisplayType,
    string? Description,
    int SortOrder,
    bool IsActive,
    string? ColorCode,
    string? IconClass
);
