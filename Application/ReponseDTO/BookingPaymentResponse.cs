namespace Application.ReponseDTO;

/// <summary>Customer's bookings that still have a due (for the booking dropdown on the payment screen).</summary>
public class CustomerDueBookingsResponse
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerMobile { get; set; }
    /// <summary>Advance received and not yet applied to a booking.</summary>
    public decimal AdvanceBalance { get; set; }
    public List<DueBookingOption> Bookings { get; set; } = [];
}

public class DueBookingOption
{
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public bool IsContracted { get; set; }
    public decimal Due { get; set; }
}

/// <summary>Everything about one booking shown on the payment screen (report-style left side + summary).</summary>
public class BookingPaymentDetailResponse
{
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public bool IsContracted { get; set; }
    public string? Notes { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerMobile { get; set; }
    public string? CustomerAddress { get; set; }

    public List<BookingPaymentItemLine> Items { get; set; } = [];
    public List<BookingPaymentDeliveryLine> Deliveries { get; set; } = [];
    public List<BookingPaymentHistoryLine> Payments { get; set; } = [];
    public List<BookingPaymentHistoryLine> AdvanceApplications { get; set; } = [];
    public List<BookingPaymentHistoryLine> Discounts { get; set; } = [];

    /// <summary>Charge ledger rows of the booking (rent per month, labour, adjustments, corrections).</summary>
    public List<LedgerEntryResponse> LedgerEntries { get; set; } = [];

    public BookingPaymentSummary Summary { get; set; } = new();
    /// <summary>Customer advance available to apply to this booking.</summary>
    public decimal AvailableAdvance { get; set; }
}

public class BookingPaymentItemLine
{
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string BillType { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal BookedQty { get; set; }
    public decimal DeliveredQty { get; set; }
    public decimal StockInStore { get; set; }
    /// <summary>Billing month currently running for the stock still in store.</summary>
    public int CurrentMonth { get; set; }
    /// <summary>Rent charged for this item so far (sum of its monthly ledger entries).</summary>
    public decimal StockRent { get; set; }
    /// <summary>Rent already charged on this item's deliveries.</summary>
    public decimal DeliveredRent { get; set; }
    public decimal BookingLabour { get; set; }
    public decimal DeliveryLabour { get; set; }
}

public class BookingPaymentDeliveryLine
{
    public string DeliveryNumber { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public float Quantity { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public int BillingCycles { get; set; }
    public decimal RentCharge { get; set; }
    public decimal LabourCharge { get; set; }
    public decimal Total { get; set; }
    public decimal CollectedAmount { get; set; }
}

public class BookingPaymentHistoryLine
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    /// <summary>AT_DELIVERY, PAYMENT, ADVANCE or DISCOUNT.</summary>
    public string Kind { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
}

public class BookingPaymentSummary
{
    public decimal RentCharge { get; set; }
    public decimal LabourCharge { get; set; }
    public decimal Adjustment { get; set; }
    /// <summary>Manual "Adjustment +" (extra charges entered on the Discount &amp; Adjustment page).</summary>
    public decimal ManualAdjustment { get; set; }
    public decimal TotalCharge { get; set; }
    public decimal PaidAtDelivery { get; set; }
    public decimal OtherPayments { get; set; }
    public decimal AdvanceApplied { get; set; }
    public decimal Discount { get; set; }
    public decimal Due { get; set; }
}

public class BookingPaymentResultResponse
{
    public string ReceiptCode { get; set; } = string.Empty;
    /// <summary>Cash/bank payment transaction (null when only advance/discount were used).</summary>
    public Guid? PaymentTransactionId { get; set; }
    public Guid? DiscountTransactionId { get; set; }
    public decimal CashAmount { get; set; }
    public decimal AdvanceApplied { get; set; }
    public decimal Discount { get; set; }
    public decimal DueAfter { get; set; }
}

public class CustomerAdvancesResponse
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalReceived { get; set; }
    public decimal TotalApplied { get; set; }
    public decimal AdvanceBalance { get; set; }
    public List<CustomerAdvanceLine> Advances { get; set; } = [];
    public List<AdvanceAllocationLine> Allocations { get; set; } = [];
}

public class CustomerAdvanceLine
{
    public Guid TransactionId { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public decimal Applied { get; set; }
    public decimal Remaining { get; set; }
}

public class AdvanceAllocationLine
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string? ReceiptCode { get; set; }
    public string AdvanceCode { get; set; } = string.Empty;
    public string BookingNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>Extra lines for printing a booking payment receipt.</summary>
public class PaymentReceiptInfoResponse
{
    public string? BookingNumber { get; set; }
    public decimal CashAmount { get; set; }
    public decimal AdvanceApplied { get; set; }
    public decimal Discount { get; set; }
    public decimal BookingDueNow { get; set; }
}
