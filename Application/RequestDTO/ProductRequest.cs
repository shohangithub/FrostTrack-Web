namespace Application.RequestDTO;

public record ProductRequest(
    int Id,
    string ProductName,
    string ProductCode,
    string? CustomBarcode,
    int CategoryId,
    int? DefaultUnitId,
    bool IsRawMaterial,
    bool IsFinishedGoods,
    string? Address,
    string? ImageUrl,
    int? ReOrederLevel,
    decimal? PurchaseRate,
    decimal? SellingRate,
    decimal? WholesalePrice,
    decimal? VatPercent,
    bool IsActive,
    bool IsProductAsService
    );