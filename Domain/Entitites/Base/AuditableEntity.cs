namespace Domain.Entitites;

public abstract class AuditableEntity<T> : BaseEntity<T>
{
    public int CreatedById { get; set; }
    [Column(TypeName = "datetime2")]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    public int? LastUpdatedById { get; set; }
    [Column(TypeName = "datetime2")]
    public DateTime? LastUpdatedTime { get; set; }

    // Soft delete — permanently hides the record from all queries.
    // Use for user-initiated "undo" deletions. Recoverable via RestoreAsync.
    public bool IsDeleted { get; set; } = false;
    [Column(TypeName = "datetime2")]
    public DateTime? DeletedAt { get; set; }
    public int? DeletedById { get; set; }

    // Archive — removes from active working views but preserves for history/reporting.
    // Use for completed or superseded records that must remain visible in ledgers.
    public bool IsArchived { get; set; } = false;
    [Column(TypeName = "datetime2")]
    public DateTime? ArchivedAt { get; set; }
    public int? ArchivedById { get; set; }
}
