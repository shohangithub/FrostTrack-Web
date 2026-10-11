using Application.ReponseDTO;

namespace Application.Contractors;

/// <summary>
/// Charge ledger (RecurringChargeEntries): the reliable, logged source of every charge on a booking.
/// Sync is idempotent — it posts only what is missing or changed.
/// </summary>
public interface IBookingLedgerService
{
    /// <summary>Posts missing charges / corrections for one booking as of <paramref name="asOf"/> (default now).</summary>
    Task<LedgerSyncResult> SyncBookingAsync(Guid bookingId, DateTime? asOf = null, Guid? runId = null, bool dryRun = false, CancellationToken cancellationToken = default);

    Task<LedgerSyncResult> SyncBookingsAsync(IEnumerable<Guid> bookingIds, DateTime? asOf = null, CancellationToken cancellationToken = default);

    /// <summary>Sync for every active booking (daily job, manual run, backfill).</summary>
    Task<LedgerSyncResult> SyncAllAsync(DateTime asOf, Guid? runId = null, bool dryRun = false, CancellationToken cancellationToken = default);

    /// <summary>Setup only: marks every current row LEGACY (kept, not counted) and posts the ledger again from scratch.</summary>
    Task<LedgerSyncResult> RebuildAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Ledger charges per booking with EntryDate &lt;= <paramref name="asOf"/>.</summary>
    Task<Dictionary<Guid, LedgerCharges>> GetChargesAsync(IEnumerable<Guid> bookingIds, DateTime asOf, CancellationToken cancellationToken = default);

    Task<List<LedgerEntryResponse>> GetEntriesAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>Posts a manual "Adjustment +" (extra charge) on a booking. Sync never touches these rows.</summary>
    Task<LedgerEntryResponse> AddManualAdjustmentAsync(Guid bookingId, decimal amount, DateTime entryDate, string reason, string? referenceCode, CancellationToken cancellationToken = default);

    /// <summary>Cancels a manual adjustment by posting the opposite amount (the ledger is append-only).</summary>
    Task<bool> ReverseManualAdjustmentAsync(Guid entryId, CancellationToken cancellationToken = default);

    /// <summary>Bookings whose ledger total differs from the independent calculation or have pending postings.</summary>
    Task<List<LedgerCheckItem>> CheckAsync(bool onlyDifferences = true, CancellationToken cancellationToken = default);
}
