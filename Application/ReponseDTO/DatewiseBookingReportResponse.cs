namespace Application.ReponseDTO;

public class DatewiseBookingReportResponse
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double BookingQuantity { get; set; }
    public decimal RentRate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Remarks { get; set; } = string.Empty;
}
