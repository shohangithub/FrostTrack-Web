using System.Collections.Concurrent;

namespace Application.Services;

/// <summary>
/// Manual recurring-charge run: posts every missing charge to the charge ledger (same sync as the
/// daily job and the booking/delivery hooks), and keeps a run log. Preview shows what would be posted.
/// </summary>
public class RecurringChargeManagementService(
    IRepository<RecurringChargeRun, Guid> recurringChargeRunRepository,
    IBookingLedgerService ledgerService,
    IUserContextService userContextService,
    ITenantProvider tenantProvider) : IRecurringChargeManagementService
{
    // Per-tenant semaphore — prevents concurrent recurring-charge runs on the same server instance.
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _runLocks = new();

    private static SemaphoreSlim GetLock(Guid tenantId) =>
        _runLocks.GetOrAdd(tenantId, _ => new SemaphoreSlim(1, 1));

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<RecurringChargePreviewResponse> PreviewAsync(
        DateTime asOfDate,
        CancellationToken cancellationToken = default)
    {
        var pending = await ledgerService.SyncAllAsync(asOfDate, dryRun: true, cancellationToken: cancellationToken);

        return new RecurringChargePreviewResponse
        {
            AsOfDate = asOfDate,
            TotalAffectedBookings = pending.BookingsAffected,
            TotalAffectedDetailLines = pending.EntriesPosted,
            TotalRecurringChargeAmount = pending.AmountPosted,
            Bookings = pending.Bookings.Select(b => new RecurringChargePreviewBookingResponse
            {
                BookingId = b.BookingId,
                BookingNumber = b.BookingNumber,
                CustomerName = b.CustomerName,
                AffectedDetailLines = b.Entries,
                TotalRecurringChargeAmount = b.Amount,
                OldestLastRecurringChargeDate = b.OldestEntryDate,
            }).ToList(),
        };
    }

    public async Task<RecurringChargeRunResponse> ApplyManualRecurringChargeAsync(
        RecurringChargeRunRequest request,
        CancellationToken cancellationToken = default)
    {
        var asOfDate = request.AsOfDate?.ToUniversalTime() ?? DateTime.UtcNow;
        var tenantId = tenantProvider.GetTenantId();
        var currentUser = userContextService.GetCurrentUser();
        var sem = GetLock(tenantId);

        // Non-blocking tryEnter — fail fast if already running.
        if (!await sem.WaitAsync(0, cancellationToken))
            throw new InvalidOperationException(
                "A recurring-charge run is already in progress for this tenant. Please wait and try again.");

        // Create the audit log entry immediately so UI can poll if needed.
        var run = new RecurringChargeRun
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TriggeredBy = RecurringChargeTriggerTypes.Manual,
            AsOfDate = asOfDate,
            Status = RecurringChargeRunStatuses.InProgress,
            Notes = request.Notes,
            RunByUserId = currentUser.Id,
            RunByUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim(),
            StartedAt = DateTime.UtcNow,
        };
        await recurringChargeRunRepository.AddAsync(run, cancellationToken);

        try
        {
            // Entries posted by this run carry its id (RecurringChargeRunId)
            var result = await ledgerService.SyncAllAsync(asOfDate, run.Id, cancellationToken: cancellationToken);

            run.Status = RecurringChargeRunStatuses.Success;
            run.AffectedCount = result.EntriesPosted;
            run.TotalRecurringChargeAmount = result.AmountPosted;
            run.CompletedAt = DateTime.UtcNow;
            await recurringChargeRunRepository.UpdateAsync(run, cancellationToken);

            return MapToResponse(run);
        }
        catch (Exception ex)
        {
            run.Status = RecurringChargeRunStatuses.Failed;
            run.ErrorMessage = ex.Message;
            run.CompletedAt = DateTime.UtcNow;
            await recurringChargeRunRepository.UpdateAsync(run, cancellationToken);
            throw;
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<List<RecurringChargeRunResponse>> GetHistoryAsync(
        int take = 30,
        CancellationToken cancellationToken = default)
    {
        var runs = await recurringChargeRunRepository.UnfilteredQuery()
            .Where(r => r.TenantId == tenantProvider.GetTenantId())
            .OrderByDescending(r => r.StartedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return runs.Select(MapToResponse).ToList();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static RecurringChargeRunResponse MapToResponse(RecurringChargeRun r) => new()
    {
        Id = r.Id,
        TriggeredBy = r.TriggeredBy,
        AsOfDate = r.AsOfDate,
        Status = r.Status,
        AffectedCount = r.AffectedCount,
        TotalRecurringChargeAmount = r.TotalRecurringChargeAmount,
        Notes = r.Notes,
        RunByUserName = r.RunByUserName,
        StartedAt = r.StartedAt,
        CompletedAt = r.CompletedAt,
        ErrorMessage = r.ErrorMessage,
    };
}
