namespace Application.ReponseDTO;

public class DeliveryResponse
{
    public Guid Id { get; set; }
    public string DeliveryNumber { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public Guid BookingId { get; set; }
    public string? BookingNumber { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? Notes { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal AdjustmentValue { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<DeliveryDetailResponse> DeliveryDetails { get; set; } = [];
}

public class DeliveryDetailResponse
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public Guid BookingDetailId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int DeliveryUnitId { get; set; }
    public string? DeliveryUnitName { get; set; }
    public float DeliveryQuantity { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal AdjustmentValue { get; set; }

    // For tracking remaining quantity
    public float BookingQuantity { get; set; }
    public float TotalDeliveredQuantity { get; set; }
    public float RemainingQuantity { get; set; }
}

public class BookingForDeliveryResponse
{
    public Guid Id { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? Notes { get; set; }
    public DateTime LastDeliveryDate { get; set; }
    public List<BookingDetailForDeliveryResponse> BookingDetails { get; set; } = [];
}

public class BookingDetailForDeliveryResponse
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int BookingUnitId { get; set; }
    public string? BookingUnitName { get; set; }
    public float BookingQuantity { get; set; }
    public string BillType { get; set; } = string.Empty;
    public decimal BookingRate { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal BaseRate { get; set; }
    public decimal TotalCharge { get; set; } // Charge per delivery unit

    // Tracking delivered quantities
    public float TotalDeliveredQuantity { get; set; }
    public float RemainingQuantity { get; set; }
    public DateTime LastDeliveryDate { get; set; }

    // Available units for conversion
    public List<DeliveryUnitConversionResponse> AvailableUnits { get; set; } = [];
}

public class DeliveryUnitConversionResponse
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public string? UnitName { get; set; }
    public decimal ConversionRate { get; set; }
    public bool IsBaseUnit { get; set; }
}

public class RemainingQuantityResponse
{
    public Guid BookingDetailId { get; set; }
    public float BookingQuantity { get; set; }
    public float TotalDeliveredQuantity { get; set; }
    public float RemainingQuantity { get; set; }
}
