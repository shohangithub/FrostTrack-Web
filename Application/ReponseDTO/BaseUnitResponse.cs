namespace Application.ReponseDTO;

public record BaseUnitResponse(int Id, string UnitName, string Description, bool IsActive, string Status);
public record BaseUnitListResponse(int Id, string UnitName, string? Description, string Status);
