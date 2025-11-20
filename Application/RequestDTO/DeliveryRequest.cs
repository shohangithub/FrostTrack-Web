namespace Application.RequestDTO;

public class CreateDeliveryRequest
{
    public required string DeliveryNumber { get; set; }
    public required DateTime DeliveryDate { get; set; }
    public required Guid BookingId { get; set; }
    public string? Notes { get; set; }
    public required decimal ChargeAmount { get; set; }
    public required decimal AdjustmentValue { get; set; }
    public List<CreateDeliveryDetailRequest> DeliveryDetails { get; set; } = [];

    // For integrated billing
    public bool CreateTransaction { get; set; } = false;
    public decimal? TransactionAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionNotes { get; set; }
}

public class CreateDeliveryDetailRequest
{
    public required Guid BookingDetailId { get; set; }
    public required int DeliveryUnitId { get; set; }
    public required float DeliveryQuantity { get; set; }
    public required decimal BaseQuantity { get; set; }
    public required decimal ChargeAmount { get; set; }
    public required decimal AdjustmentValue { get; set; }
}

public class UpdateDeliveryRequest
{
    public required string DeliveryNumber { get; set; }
    public required DateTime DeliveryDate { get; set; }
    public string? Notes { get; set; }
    public required decimal ChargeAmount { get; set; }
    public required decimal AdjustmentValue { get; set; }
    public List<UpdateDeliveryDetailRequest> DeliveryDetails { get; set; } = [];
}

public class UpdateDeliveryDetailRequest
{
    public Guid? Id { get; set; } // Null for new items
    public required Guid BookingDetailId { get; set; }
    public required int DeliveryUnitId { get; set; }
    public required float DeliveryQuantity { get; set; }
    public required decimal BaseQuantity { get; set; }
    public required decimal ChargeAmount { get; set; }
    public required decimal AdjustmentValue { get; set; }
}
