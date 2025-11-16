namespace Application.ReponseDTO;

public record struct TokenResponse(string Token, string? Email, string? FirstName, string? LastName);
