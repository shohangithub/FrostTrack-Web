namespace Application.ReponseDTO;

/// <summary>
/// Everything that happened on one booking: booking info, items, deliveries (with challan),
/// a date-ordered account statement with running due, adjustments and the due summary.
/// Figures come from the same source as the payment screen (charge ledger + payments).
/// </summary>
public class BookingHistoryResponse
{
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public DateTime BookingDate { get; set; }
    public bool IsContracted { get; set; }
    public string? Notes { get; set; }
    public string? BranchName { get; set; }
    /// <summary>RUNNING (stock in store) or COMPLETED (fully delivered).</summary>
    public string Status { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerMobile { get; set; }
    public string? CustomerAddress { get; set; }

    public List<BookingPaymentItemLine> Items { get; set; } = [];
    public List<BookingHistoryDelivery> Deliveries { get; set; } = [];
    public List<BookingStatementLine> Statement { get; set; } = [];
    /// <summary>All Adjustment + / − including cancelled ones.</summary>
    public List<BookingAdjustmentLine> Adjustments { get; set; } = [];
    public BookingPaymentSummary Summary { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class BookingHistoryDelivery
{
    public Guid DeliveryId { get; set; }
    public string DeliveryNumber { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public string? Notes { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? ChallanNumber { get; set; }
    public string? VehicleNumber { get; set; }
    public string? DriverName { get; set; }
    public decimal AdjustmentValue { get; set; }
    public decimal CollectedAmount { get; set; }
    public List<BookingHistoryDeliveryLine> Lines { get; set; } = [];
}

public class BookingHistoryDeliveryLine
{
    public string ProductName { get; set; } = string.Empty;
    public float Quantity { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public int BillingCycles { get; set; }
    public decimal RentCharge { get; set; }
    public decimal LabourCharge { get; set; }
}

/// <summary>One line of the account statement: a charge (raises the due) or money in (lowers it).</summary>
public class BookingStatementLine
{
    public DateTime Date { get; set; }
    /// <summary>RENT, LABOUR, DELIVERY_ADJUSTMENT, ADJUSTMENT_PLUS, CORRECTION, PAYMENT, AT_DELIVERY, ADVANCE, ADJUSTMENT_MINUS</summary>
    public string Category { get; set; } = string.Empty;
    public string Particulars { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public decimal Charge { get; set; }
    public decimal Paid { get; set; }
    public decimal Balance { get; set; }
}
