namespace Application.ReponseDTO;

public record DeliveryResponse
{
    public Guid Id { get; init; }
    public string DeliveryNumber { get; init; } = string.Empty;
    public DateTime DeliveryDate { get; init; }
    public Guid BookingId { get; init; }
    public BookingResponse Booking { get; init; } = null!;
    public int BranchId { get; init; }
    public BranchResponse Branch { get; init; } = null!;
    public string? Notes { get; init; }
    public decimal ChargeAmount { get; init; }
    public decimal AdjustmentValue { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public List<DeliveryDetailResponse> DeliveryDetails { get; init; } = new();
}

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

public record DeliveryDetailResponse
{
    public Guid Id { get; init; }
    public Guid BookingDetailId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int DeliveryUnitId { get; init; }
    public string DeliveryUnitName { get; init; } = string.Empty;
    public float DeliveryQuantity { get; init; }
    public decimal BaseQuantity { get; init; }
    public decimal ChargeAmount { get; init; }
    public decimal AdjustmentValue { get; init; }
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
