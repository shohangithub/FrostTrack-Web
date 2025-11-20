namespace Application.ReponseDTO;

public record DeliveryListResponse
{
    public Guid Id { get; init; }
    public string DeliveryNumber { get; init; } = string.Empty;
    public DateTime DeliveryDate { get; init; }
    public Guid BookingId { get; init; }
    public string BookingNumber { get; init; } = string.Empty;
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public decimal ChargeAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public List<DeliveryDetailResponse> DeliveryDetails { get; init; } = new();
}

public record CustomerStockResponse
{
    public int CustomerId { get; init; }
    public Guid BookingDetailId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int UnitId { get; init; }
    public string UnitName { get; init; } = string.Empty;
    public decimal AvailableStock { get; init; }
    public decimal BookingRate { get; init; }
}
