namespace Application.RequestDTO;

public record DeliveryRequest
{
    public Guid Id { get; init; } = Guid.Empty;
    public string DeliveryNumber { get; init; } = string.Empty;
    public DateTime DeliveryDate { get; init; }
    public Guid BookingId { get; init; }
    public int BranchId { get; init; }
    public string? Notes { get; init; }
    public decimal ChargeAmount { get; init; }
    public decimal AdjustmentValue { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public List<DeliveryDetailRequest> DeliveryDetails { get; init; } = new();
}

public record DeliveryDetailRequest
{
    public Guid Id { get; init; } = Guid.Empty;
    public Guid DeliveryId { get; init; } = Guid.Empty;
    public Guid BookingDetailId { get; init; }
    public int DeliveryUnitId { get; init; }
    public float DeliveryQuantity { get; init; }
    public decimal BaseQuantity { get; init; }
    public decimal ChargeAmount { get; init; }
    public decimal AdjustmentValue { get; init; }
}
