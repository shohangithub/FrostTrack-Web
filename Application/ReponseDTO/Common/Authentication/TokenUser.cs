namespace Application.ReponseDTO;

public record struct TokenUser(int id, Guid tenantId, string email, string name, int? branchId, IList<string> roles, string? profileImageUrl = null, List<string>? permissions = null);
