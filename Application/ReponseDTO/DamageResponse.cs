namespace Application.ReponseDTO;

public record DamageResponse(
    int Id,
    string DamageNumber,
    DateTime DamageDate,
    int ProductId,
    string? ProductName,
    int UnitId,
    string? UnitName,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost,
    string? Reason,
    string? Description,
    bool IsActive,
    string Status
);

public record DamageListResponse(
    int Id,
    string DamageNumber,
    DateTime DamageDate,
    string? ProductName,
    string? UnitName,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost,
    string? Reason,
    string Status
);