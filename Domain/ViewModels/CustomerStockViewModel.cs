namespace Domain.ViewModels;

public class CustomerStockViewModel
{
    public int CustomerId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal AvailableStock { get; set; }
    public decimal BookingRate { get; set; }
}
