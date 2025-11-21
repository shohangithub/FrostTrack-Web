namespace Application.ReponseDTO;

public class DeliveryInvoiceResponse
{
    public Guid Id { get; set; }
    public string DeliveryNumber { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? Notes { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal AdjustmentValue { get; set; }

    // Customer Information
    public CustomerBasicInfo? Customer { get; set; }

    // Booking Information
    public BookingInvoiceInfo? Booking { get; set; }

    // Delivery Details
    public List<DeliveryInvoiceDetailResponse> DeliveryDetails { get; set; } = [];

    // Financial Summary
    public decimal TotalBookingAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal ExtraCharge { get; set; }
    public decimal DueAmount { get; set; }
}

public class BookingInvoiceInfo
{
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public DateTime LastDeliveryDate { get; set; }
    public decimal TotalBookingAmount { get; set; }
}

public class DeliveryInvoiceDetailResponse
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int DeliveryUnitId { get; set; }
    public string DeliveryUnitName { get; set; } = string.Empty;
    public float DeliveryQuantity { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal BookingRate { get; set; }
}
