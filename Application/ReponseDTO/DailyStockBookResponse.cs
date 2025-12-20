namespace Application.ReponseDTO;

public class DailyStockBookItemResponse
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double PreviousStock { get; set; }
    public double TotalBooking { get; set; }
    public double TotalDelivery { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public double CurrentStock { get; set; }
    public decimal ReceivedRent { get; set; }
}
