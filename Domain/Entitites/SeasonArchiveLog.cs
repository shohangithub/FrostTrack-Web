using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entitites;

/// <summary>
/// Immutable audit log for every Season Closing & Batch Archive run.
/// Captures counts, financial totals, and carried forward "rest data" snapshots.
/// </summary>
[Table("SeasonArchiveLogs", Schema = "operations")]
public class SeasonArchiveLog : BaseEntity<Guid>
{
    [MaxLength(50)]
    public string BatchCode { get; set; } = string.Empty;

    [MaxLength(200)]
    public string SeasonTitle { get; set; } = string.Empty;

    [Column(TypeName = "datetime2")]
    public DateTime CutoffDate { get; set; }

    public int TotalArchivedBookings { get; set; }
    public int TotalCarriedForwardBookings { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCarriedForwardStock { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCarriedForwardCustomerDue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCarriedForwardCashBalance { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCarriedForwardBankBalance { get; set; }

    public int TotalArchivedDeliveries { get; set; }
    public int TotalArchivedTransactions { get; set; }
    public int TotalArchivedChallans { get; set; }

    public int ExecutedById { get; set; }

    [MaxLength(200)]
    public string ExecutedByName { get; set; } = string.Empty;

    [Column(TypeName = "datetime2")]
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }
}
