namespace Application.ReponseDTO;

public record AssetResponse(
    int Id,
    string AssetName,
    string AssetCode,
    string? AssetType,
    string? SerialNumber,
    string? Model,
    string? Manufacturer,
    DateTime? PurchaseDate,
    decimal PurchaseCost,
    decimal CurrentValue,
    decimal DepreciationRate,
    string? Location,
    string? Department,
    string? AssignedTo,
    string? Condition,
    DateTime? WarrantyExpiryDate,
    DateTime? MaintenanceDate,
    string? Notes,
    string? ImageUrl,
    bool IsActive,
    string Status
    );

public record AssetListResponse(
    int Id,
    string AssetName,
    string AssetCode,
    string? AssetType,
    string? SerialNumber,
    string? Model,
    string? Manufacturer,
    DateTime? PurchaseDate,
    decimal PurchaseCost,
    decimal CurrentValue,
    decimal DepreciationRate,
    string? Location,
    string? Department,
    string? AssignedTo,
    string? Condition,
    DateTime? WarrantyExpiryDate,
    DateTime? MaintenanceDate,
    string? Notes,
    string? ImageUrl,
    string Status
    );