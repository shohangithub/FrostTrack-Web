namespace Application.ReponseDTO;

public record UserClaimsResponse(int Id, string UserName, string Email, IEnumerable<ClaimResponse> Claims);
public record ClaimResponse(string Type, string Value);
