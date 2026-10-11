namespace Application.ReponseDTO;

/// <summary>
/// Period bill statement for a customer who settles bookings and deliveries monthly.
/// </summary>
public class CustomerBillStatementResponse
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerMobile { get; set; }
    public string? Address { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    // Stock in store and storage rent per booking line (sums to RentCharged together with DeliveryAdjustmentTotal)
    public List<BillStatementStockLine> StockLines { get; set; } = [];
    public List<BillStatementBookingLine> Bookings { get; set; } = [];
    public List<BillStatementDeliveryLine> Deliveries { get; set; } = [];
    public List<BillStatementPaymentLine> Payments { get; set; } = [];
    public List<BillStatementPaymentLine> Discounts { get; set; } = [];

    /// <summary>Monthly bills posted in the period: rent months and labour (never corrections).</summary>
    public List<LedgerEntryResponse> ChargeEntries { get; set; } = [];

    /// <summary>Corrections of earlier charges, delivery adjustments and Adjustment + dated in the period.</summary>
    public List<BillStatementCorrectionLine> Corrections { get; set; } = [];

    // Line totals (informational)
    public decimal BookingRentTotal { get; set; }
    public decimal BookingLabourTotal { get; set; }
    public decimal DeliveryRentTotal { get; set; }
    public decimal DeliveryLabourTotal { get; set; }
    public decimal DeliveryAdjustmentTotal { get; set; }
    public decimal DeliveryCollectedTotal { get; set; }

    // Account summary (same accrual rules as the customer due report)
    public decimal PreviousDue { get; set; }
    /// <summary>Rent of the months billed in the period (posted bills only).</summary>
    public decimal RentCharged { get; set; }
    /// <summary>Labour billed in the period (posted bills only).</summary>
    public decimal LabourCharged { get; set; }
    /// <summary>Corrections + delivery adjustments + Adjustment + in the period (may be negative).</summary>
    public decimal CorrectionsTotal { get; set; }
    /// <summary>RentCharged + LabourCharged + CorrectionsTotal.</summary>
    public decimal TotalCharged { get; set; }
    public decimal TotalPaid { get; set; }
    /// <summary>Part of TotalPaid collected together with a delivery.</summary>
    public decimal PaidAtDelivery { get; set; }
    /// <summary>Part of TotalPaid received separately (bill collection, advance, ...).</summary>
    public decimal OtherPayments { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal ClosingDue { get; set; }
}

public class BillStatementStockLine
{
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string BillType { get; set; } = string.Empty;
    public decimal Rate { get; set; }

    // Quantities in the booking unit
    public decimal OpeningStock { get; set; }
    public decimal BookedQty { get; set; }
    public decimal DeliveredQty { get; set; }
    public decimal ClosingStock { get; set; }

    /// <summary>Billing-cycle dates that fall inside the period (booking date counts as the first cycle).</summary>
    public List<DateTime> BillingDates { get; set; } = [];
    public int CyclesInPeriod { get; set; }

    /// <summary>Rent of the months billed for this line in the period (posted bills only, never negative).</summary>
    public decimal RentThisPeriod { get; set; }
}

/// <summary>One correction / adjustment line (corrections are grouped per booking and reason).</summary>
public class BillStatementCorrectionLine
{
    public DateTime Date { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    /// <summary>GO_LIVE, DATA_CHANGE, DELIVERY_ADJUSTMENT, ADJUSTMENT_PLUS, ADJUSTMENT_PLUS_CANCELLED</summary>
    public string Category { get; set; } = string.Empty;
    public string Particulars { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public decimal Amount { get; set; }
}

public class BillStatementBookingLine
{
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public float Quantity { get; set; }
    public string BillType { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal RentAmount { get; set; }
    public decimal LabourCharge { get; set; }
    public decimal Total { get; set; }
}

public class BillStatementDeliveryLine
{
    public Guid DeliveryId { get; set; }
    public string DeliveryNumber { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public float Quantity { get; set; }
    public int BillingCycles { get; set; }
    public decimal Rate { get; set; }
    public decimal RentCharge { get; set; }
    public decimal LabourCharge { get; set; }
    public decimal Total { get; set; }
    /// <summary>Amount taken at the time of delivery for this line.</summary>
    public decimal CollectedAmount { get; set; }
    public decimal DueAmount { get; set; }
}

public class BillStatementPaymentLine
{
    public Guid TransactionId { get; set; }
    /// <summary>Set when the payment was collected with a delivery.</summary>
    public Guid? DeliveryId { get; set; }
    public string? DeliveryNumber { get; set; }
    public DateTime Date { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
}
