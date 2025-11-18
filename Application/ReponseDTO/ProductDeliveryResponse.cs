namespace Application.ReponseDTO;

public record ProductDeliveryResponse
{
    public int Id { get; init; }
    public string DeliveryNumber { get; init; } = string.Empty;
    public DateTime DeliveryDate { get; init; }
    public int CustomerId { get; init; }
    public CustomerResponse Customer { get; init; } = null!;
    public int BranchId { get; init; }
    public string? Notes { get; init; }
    public List<ProductDeliveryDetailResponse> ProductDeliveryDetails { get; init; } = new();
}

public record ProductDeliveryListResponse
{
    public int Id { get; init; }
    public string DeliveryNumber { get; init; } = string.Empty;
    public DateTime DeliveryDate { get; init; }
    public CustomerResponse Customer { get; init; } = null!;
    public List<ProductDeliveryDetailResponse> ProductDeliveryDetails { get; init; } = new();
}

public record ProductDeliveryDetailResponse
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public ProductResponse Product { get; init; } = null!;
    public int DeliveryUnitId { get; init; }
    public UnitConversionResponse DeliveryUnit { get; init; } = null!;
    public float DeliveryQuantity { get; init; }
    public decimal DeliveryRate { get; init; }
    public decimal BaseQuantity { get; init; }
    public decimal BaseRate { get; init; }
}

public record CustomerStockResponse
{
    public int CustomerId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int UnitId { get; init; }
    public string UnitName { get; init; } = string.Empty;
    public decimal AvailableStock { get; init; }
    public decimal BookingRate { get; init; }
}
