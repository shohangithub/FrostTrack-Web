namespace Application.RequestDTO;

public record ProductDeliveryRequest
{
    public int Id { get; init; }
    public string DeliveryNumber { get; init; } = string.Empty;
    public DateTime DeliveryDate { get; init; }
    public int CustomerId { get; init; }
    public int BranchId { get; init; }
    public decimal Subtotal { get; init; }
    public decimal VatAmount { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal OtherCost { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public string? Notes { get; init; }
    public List<ProductDeliveryDetailRequest> ProductDeliveryDetails { get; init; } = new();
}

public record ProductDeliveryDetailRequest
{
    public int Id { get; init; }
    public int ProductDeliveryId { get; init; }
    public int ProductId { get; init; }
    public int DeliveryUnitId { get; init; }
    public decimal DeliveryQuantity { get; init; }
    public decimal BookingRate { get; init; }
    public decimal DeliveryAmount { get; init; }
}
