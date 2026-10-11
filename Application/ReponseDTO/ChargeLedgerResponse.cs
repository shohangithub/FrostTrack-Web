namespace Application.ReponseDTO;

/// <summary>Charges of a booking from the charge ledger, as of a date.</summary>
public class LedgerCharges
{
    /// <summary>Rent of the first billing month (charged at booking).</summary>
    public decimal RentFirstMonth { get; set; }
    /// <summary>Rent of later billing months (the recurring part).</summary>
    public decimal RentLaterMonths { get; set; }
    public decimal BookingLabour { get; set; }
    public decimal DeliveryLabour { get; set; }
    public decimal Adjustment { get; set; }
    /// <summary>Manual "Adjustment +" entries (net of cancellations).</summary>
    public decimal ManualAdjustment { get; set; }

    public decimal Rent => RentFirstMonth + RentLaterMonths;
    public decimal Labour => BookingLabour + DeliveryLabour;
    public decimal Total => Rent + Labour + Adjustment + ManualAdjustment;
}

public class LedgerSyncResult
{
    public int BookingsAffected { get; set; }
    public int EntriesPosted { get; set; }
    public decimal AmountPosted { get; set; }
    /// <summary>Rent entries posted (or pending in a dry run) per booking.</summary>
    public List<LedgerPendingBooking> Bookings { get; set; } = [];
}

public class LedgerPendingBooking
{
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int Entries { get; set; }
    public decimal Amount { get; set; }
    public DateTime? OldestEntryDate { get; set; }
}

public class LedgerEntryResponse
{
    public Guid Id { get; set; }
    public DateTime EntryDate { get; set; }
    public string ChargeType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public int? CycleNo { get; set; }
    public Guid? BookingDetailId { get; set; }
    public string? BookingNumber { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid? DeliveryId { get; set; }
    public string? DeliveryNumber { get; set; }
    public DateTime BillPeriodFrom { get; set; }
    public DateTime BillPeriodTo { get; set; }
    public float Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    /// <summary>Voucher number of a manual adjustment.</summary>
    public string? ReferenceCode { get; set; }
    /// <summary>Set on a REVERSAL entry: the manual adjustment it cancels.</summary>
    public Guid? ReversalOfId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Ledger total compared with the independent calculation for one booking.</summary>
public class LedgerCheckItem
{
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal LedgerTotal { get; set; }
    /// <summary>Independent calculation; null for bookings dated before the go-live date.</summary>
    public decimal? CalculatedTotal { get; set; }
    public decimal Difference { get; set; }
    /// <summary>Amount the next sync would post (missing months, changed data).</summary>
    public decimal PendingSync { get; set; }
}
