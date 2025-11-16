namespace Application.ReponseDTO;

public record CurrentUser(
    int Id,
    Guid TenantId,
    string FirstName,
    string LastName,
    string Email,
    int BranchId,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> Roles);
