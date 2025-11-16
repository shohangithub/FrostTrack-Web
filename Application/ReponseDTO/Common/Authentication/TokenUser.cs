namespace Application.ReponseDTO;

public record struct TokenUser(int id, Guid tenantId, string email, string firstName, string lastName, int? branchId, IList<string> roles, List<string>? permissions = null);
