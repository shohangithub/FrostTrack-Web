namespace Application.RequestDTO;

public record UnitConversionRequest(string UnitName, int BaseUnitId, float ConversionValue, string Description, bool IsActive);