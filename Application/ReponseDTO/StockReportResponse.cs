namespace Application.ReponseDTO;

public class StockReportItemResponse
{
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double BookingQuantity { get; set; }
    public double DeliveredQuantity { get; set; }
    public double RemainingQuantity { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal BookingRate { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime? LastDeliveryDate { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Partial, Completed
}

public class StockSummaryResponse
{
    public int TotalBookings { get; set; }
    public int TotalProducts { get; set; }
    public double TotalBookedQuantity { get; set; }
    public double TotalDeliveredQuantity { get; set; }
    public double TotalRemainingQuantity { get; set; }
    public decimal TotalValue { get; set; }
}

public class CustomerStockReportResponse
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<StockReportItemResponse> Items { get; set; } = new();
    public CustomerStockSummary Summary { get; set; } = new();
}

public class CustomerStockSummary
{
    public double TotalBookedQuantity { get; set; }
    public double TotalDeliveredQuantity { get; set; }
    public double TotalRemainingQuantity { get; set; }
    public decimal TotalValue { get; set; }
}

public class ProductStockReportResponse
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public List<StockReportItemResponse> Items { get; set; } = new();
    public ProductStockSummary Summary { get; set; } = new();
}

public class ProductStockSummary
{
    public double TotalBookedQuantity { get; set; }
    public double TotalDeliveredQuantity { get; set; }
    public double TotalRemainingQuantity { get; set; }
    public decimal TotalValue { get; set; }
}
