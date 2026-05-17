namespace Application.ReponseDTO;

/// <summary>Per-booking summary shown in the preview (read-only, no changes made).</summary>
public class RecurringChargePreviewBookingResponse
{
    public Guid BookingId { get; set; }
    public string BookingNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    /// <summary>Number of BookingDetail lines that have at least one new cycle.</summary>
    public int AffectedDetailLines { get; set; }
    public decimal TotalRecurringChargeAmount { get; set; }
    /// <summary>Oldest LastRecurringChargeDate across all affected details (the "from" boundary).</summary>
    public DateTime? OldestLastRecurringChargeDate { get; set; }
}

/// <summary>Full preview payload returned before the user confirms Apply.</summary>
public class RecurringChargePreviewResponse
{
    public DateTime AsOfDate { get; set; }
    public int TotalAffectedBookings { get; set; }
    public int TotalAffectedDetailLines { get; set; }
    public decimal TotalRecurringChargeAmount { get; set; }
    public List<RecurringChargePreviewBookingResponse> Bookings { get; set; } = [];
}

/// <summary>Returned after a completed (or failed) recurring-charge run.</summary>
public class RecurringChargeRunResponse
{
    public Guid Id { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;
    public DateTime AsOfDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AffectedCount { get; set; }
    public decimal TotalRecurringChargeAmount { get; set; }
    public string? Notes { get; set; }
    public string RunByUserName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
