namespace Application.RequestDTO;

public record TransactionHeadRequest(
    string Name,
    string Type,
    string? DisplayType,
    string? Description,
    int SortOrder,
    bool IsActive,
    string? ColorCode,
    string? IconClass
);
