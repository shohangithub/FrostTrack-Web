namespace Application.ReponseDTO;

public class CustomerDueSummaryResponse
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalAmount { get; set; }       // total accrued (opening + all billing cycles delivered + pending)
    public decimal PendingRecurringChargeAmount { get; set; } // cycles accrued but not yet covered by a delivery record
    /// <summary>Paid on the listed bookings (payments linked to them + advance applied to them).</summary>
    public decimal TotalPaid { get; set; }
    public decimal TotalDiscount { get; set; }
    /// <summary>Customer advance not applied to any booking yet (reduces the due).</summary>
    public decimal AdvanceBalance { get; set; }
    /// <summary>TotalAmount − TotalPaid − TotalDiscount − AdvanceBalance (never below 0).</summary>
    public decimal TotalDue { get; set; }
    public DateTime OldestBookingDate { get; set; }
    public int DaysSinceOldestBooking { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public decimal? LastPaymentAmount { get; set; }
    public int DaysSinceLastPayment { get; set; }
    public string Status { get; set; } = "normal"; // normal, warning, danger
}

public class CustomerDueDetailResponse
{
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string? ReferenceNumber { get; set; }
    /// <summary>Booking (inward) labour charged in the ledger.</summary>
    public decimal BookingLabourCharge { get; set; }
    /// <summary>Delivery labour charged in the ledger.</summary>
    public decimal DeliveryLabourCharge { get; set; }
    public decimal TotalRentAmount { get; set; }
    public decimal TotalLabourAmount { get; set; }
    /// <summary>Manual "Adjustment +" (extra charges from the Discount &amp; Adjustment page); part of TotalAccruedAmount.</summary>
    public decimal ManualAdjustment { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalAccruedAmount { get; set; }  // delivery charges + pending accrual
    public decimal PendingRecurringChargeAmount { get; set; } // cycles since last delivery not yet recorded
    public DateTime? LastDeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }          // kept for backward-compat (== TotalAccruedAmount)
    public decimal TotalPaid { get; set; }
    public decimal TotalDiscount { get; set; }
    /// <summary>Charges − paid − discount; negative means the booking is in credit.</summary>
    public decimal TotalDue { get; set; }
    public int DaysSinceBooking { get; set; }
    public string Status { get; set; } = "normal"; // normal, warning, danger
    public List<CustomerDueDeliveryResponse> Deliveries { get; set; } = [];
    public List<RecurringChargeEntryResponse> RecurringChargeEntries { get; set; } = [];
}

public class CustomerDueDeliveryResponse
{
    public Guid DeliveryId { get; set; }
    public string DeliveryNumber { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal LabourCharge { get; set; }
    public decimal AdjustmentValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public List<DeliveryDetailInfoResponse> DeliveryDetails { get; set; } = [];
}

public class CustomerOutstandingResponse
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal TotalAccrued { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalDue { get; set; }
    public List<BookingOutstandingItem> Bookings { get; set; } = [];
}

public class BookingOutstandingItem
{
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public decimal AccruedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
}
