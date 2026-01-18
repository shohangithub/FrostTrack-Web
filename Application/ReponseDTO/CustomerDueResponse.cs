namespace Application.ReponseDTO;

public class CustomerDueSummaryResponse
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalDue { get; set; }
    public DateTime OldestBookingDate { get; set; }
    public int DaysSinceOldestBooking { get; set; }
    public string Status { get; set; } = "normal"; // normal, warning, danger
}

public class CustomerDueDetailResponse
{
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalDue { get; set; }
    public int DaysSinceBooking { get; set; }
    public string Status { get; set; } = "normal"; // normal, warning, danger
    public List<CustomerDueDeliveryResponse> Deliveries { get; set; } = [];
}

public class CustomerDueDeliveryResponse
{
    public Guid DeliveryId { get; set; }
    public string DeliveryNumber { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal LabourCharge { get; set; }
    public decimal AdjustmentValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public List<DeliveryDetailInfoResponse> DeliveryDetails { get; set; } = [];
}
