namespace Application.ReponseDTO;

public static class AdjustmentDirections
{
    public const string Plus = "PLUS";
    public const string Minus = "MINUS";
}

public static class AdjustmentSources
{
    /// <summary>Adjustment − : BILL_DISCOUNT transaction.</summary>
    public const string Discount = "DISCOUNT";
    /// <summary>Adjustment + : MANUAL_ADJUSTMENT charge-ledger row.</summary>
    public const string Ledger = "LEDGER";
}

/// <summary>One Adjustment + / − on a booking (or on the customer when BookingId is null).</summary>
public class BookingAdjustmentLine
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? ReferenceCode { get; set; }
    public Guid? BookingId { get; set; }
    public string? BookingNumber { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedTime { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
}

public class BookingAdjustmentResultResponse
{
    public string Direction { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal DueBefore { get; set; }
    public decimal DueAfter { get; set; }
}
