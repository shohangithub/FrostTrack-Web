namespace Application.RequestDTO;

public record ProductDeliveryRequest
{
    public int Id { get; init; }
    public string DeliveryNumber { get; init; } = string.Empty;
    public DateTime DeliveryDate { get; init; }
    public int CustomerId { get; init; }
    public int BranchId { get; init; }
    public string? Notes { get; init; }
    public List<ProductDeliveryDetailRequest> ProductDeliveryDetails { get; init; } = new();
}

public record ProductDeliveryDetailRequest
{
    public int Id { get; init; }
    public int ProductDeliveryId { get; init; }
    public int ProductId { get; init; }
    public int DeliveryUnitId { get; init; }
    public float DeliveryQuantity { get; init; }
    public decimal DeliveryRate { get; init; }
    public decimal BaseQuantity { get; init; }
    public decimal BaseRate { get; init; }
}
