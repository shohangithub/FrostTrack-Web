namespace Application.RequestDTO;

public record TransactionGroupRequest(
    string Name,
    int SortOrder,
    string? Description,
    bool IsActive
);
