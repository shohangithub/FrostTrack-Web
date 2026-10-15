namespace Application.RequestDTO;

public record TransactionGroupRequest(
    string Name,
    int SortOrder,
    string? Description,
    bool IsActive,
    string? AccountType = null
);
