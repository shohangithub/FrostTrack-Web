namespace Application.RequestDTO;

public record ProductRequest(
    int Id,
    string ProductName,
    string ProductCode,
    string? CustomBarcode,
    int CategoryId,
    int? DefaultUnitId,
    string? Address,
    string? ImageUrl,
    decimal? BookingRate,
    bool IsActive
    );