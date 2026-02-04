namespace Application.ReponseDTO;

public class DatewiseDeliveryReportResponse
{
    public Guid DeliveryId { get; set; }
    public string DeliveryCode { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double DeliveryQuantity { get; set; }
    public string DeliveryBy { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
}
