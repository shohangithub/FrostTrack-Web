namespace Application.ReponseDTO;

/// <summary>
/// Day-wise bookings and/or deliveries for a date range (single day = same from/to), optionally for one customer.
/// Backs the Booking, Delivery and Booking &amp; Delivery reports.
/// </summary>
public class DatewiseActivityReportResponse
{
    public string FromDate { get; set; } = string.Empty; // yyyy-MM-dd (local)
    public string ToDate { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public bool IncludesBookings { get; set; }
    public bool IncludesDeliveries { get; set; }

    public List<ActivityDay> Days { get; set; } = [];
    public ActivityTotals Totals { get; set; } = new();
}

public class ActivityDay
{
    public string Date { get; set; } = string.Empty; // yyyy-MM-dd (local)
    public List<ActivityBookingLine> Bookings { get; set; } = [];
    public List<ActivityDeliveryLine> Deliveries { get; set; } = [];
    public ActivityTotals Totals { get; set; } = new();
}

public class ActivityTotals
{
    public int BookingCount { get; set; }
    public decimal BookingRent { get; set; }
    public decimal BookingLabour { get; set; }
    public decimal BookingTotal { get; set; }

    public int DeliveryCount { get; set; }
    public decimal DeliveryRent { get; set; }
    public decimal DeliveryLabour { get; set; }
    public decimal DeliveryTotal { get; set; }
    public decimal DeliveryCollected { get; set; }
    public decimal DeliveryDue { get; set; }
}

public class ActivityBookingLine
{
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerMobile { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public float Quantity { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal RentAmount { get; set; }
    public decimal LabourCharge { get; set; }
    public decimal Total { get; set; }
}

public class ActivityDeliveryLine
{
    public Guid DeliveryId { get; set; }
    public string DeliveryNumber { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerMobile { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public float Quantity { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public int BillingCycles { get; set; }
    public decimal RentCharge { get; set; }
    public decimal LabourCharge { get; set; }
    public decimal Total { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal DueAmount { get; set; }
}
