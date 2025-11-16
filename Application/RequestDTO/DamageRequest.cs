namespace Application.RequestDTO;

public record DamageRequest(
    string DamageNumber,
    DateTime DamageDate,
    int ProductId,
    int UnitId,
    decimal Quantity,
    decimal UnitCost,
    decimal TotalCost,
    string? Reason,
    string? Description,
    bool IsActive
);