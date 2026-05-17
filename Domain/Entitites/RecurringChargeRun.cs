namespace Domain.Entitites;

/// <summary>
/// Immutable audit record for every recurring-charge run (auto or manual).
/// Never updated after creation — new rows only.
/// </summary>
[Table("RecurringChargeRuns", Schema = "product")]
public class RecurringChargeRun : BaseEntity<Guid>
{
    /// <summary>"AUTO" (scheduled job) or "MANUAL" (user-triggered).</summary>
    public string TriggeredBy { get; set; } = RecurringChargeTriggerTypes.Manual;

    /// <summary>The reference "as-of" date used for cycle calculations.</summary>
    [Column(TypeName = "datetime2")]
    public DateTime AsOfDate { get; set; }

    /// <summary>IN_PROGRESS → SUCCESS | FAILED.</summary>
    public string Status { get; set; } = RecurringChargeRunStatuses.InProgress;

    /// <summary>Number of BookingDetail rows whose LastRecurringChargeDate was advanced.</summary>
    public int AffectedCount { get; set; }

    /// <summary>Total computed recurring-charge money across all affected details.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalRecurringChargeAmount { get; set; }

    /// <summary>Optional operator note entered at time of manual run.</summary>
    public string? Notes { get; set; }

    public int RunByUserId { get; set; }

    [MaxLength(200)]
    public string RunByUserName { get; set; } = string.Empty;

    [Column(TypeName = "datetime2")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>Populated only when Status = FAILED.</summary>
    public string? ErrorMessage { get; set; }
}
