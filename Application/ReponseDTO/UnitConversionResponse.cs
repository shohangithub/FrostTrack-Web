namespace Application.ReponseDTO;

public record UnitConversionResponse(int Id, string UnitName, int BaseUnitId, float ConversionValue, string Description, bool IsActive, string Status);
public record UnitConversionListResponse(int Id, string UnitName, int BaseUnitId, string BaseUnitName, float ConversionValue, string? Description, string Status);
