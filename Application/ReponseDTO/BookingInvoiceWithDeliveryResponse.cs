using Domain.Entitites;

namespace Application.ReponseDTO;

public class BookingInvoiceWithDeliveryResponse
{
    public Guid Id { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    public string? Notes { get; set; }
    public List<BookingDetailWithDeliveryResponse> BookingDetails { get; set; } = [];
    public List<DeliveryInfoResponse> Deliveries { get; set; } = [];
}

public class BookingDetailWithDeliveryResponse
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public int ProductId { get; set; }
    public ProductResponse? Product { get; set; }
    public int BookingUnitId { get; set; }
    public UnitConversionResponse? BookingUnit { get; set; }
    public float BookingQuantity { get; set; }
    public string BillType { get; set; } = string.Empty;
    public decimal BookingRate { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal BaseRate { get; set; }
    public DateTime LastDeliveryDate { get; set; }
}

public class DeliveryInfoResponse
{
    public Guid Id { get; set; }
    public string DeliveryNumber { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal AdjustmentValue { get; set; }
    public List<DeliveryDetailInfoResponse> DeliveryDetails { get; set; } = [];
}

public class DeliveryDetailInfoResponse
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int DeliveryUnitId { get; set; }
    public string DeliveryUnitName { get; set; } = string.Empty;
    public float DeliveryQuantity { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal ChargeAmount { get; set; }
}
