using System.Collections.Concurrent;

namespace Application.Services;

public class RecurringChargeManagementService(
    IRepository<Booking, Guid> bookingRepository,
    IRepository<BookingDetail, Guid> bookingDetailRepository,
    IRepository<RecurringChargeRun, Guid> recurringChargeRunRepository,
    IRepository<RecurringChargeEntry, Guid> recurringChargeEntryRepository,
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
        var bookings = await LoadActiveBookingsAsync(asOfDate, cancellationToken);
        var items = ComputePreviewItems(bookings, asOfDate);

        return new RecurringChargePreviewResponse
        {
            AsOfDate = asOfDate,
            TotalAffectedBookings = items.Count,
            TotalAffectedDetailLines = items.Sum(i => i.AffectedDetailLines),
            TotalRecurringChargeAmount = items.Sum(i => i.TotalRecurringChargeAmount),
            Bookings = items,
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
            var bookings = await LoadActiveBookingsAsync(asOfDate, cancellationToken);
            int affected = 0;
            decimal totalAmount = 0m;

            foreach (var booking in bookings)
            {
                foreach (var detail in booking.BookingDetails.Where(d => !d.IsDeleted))
                {
                    var fromDate = detail.LastRecurringChargeDate ?? booking.BookingDate;
                    int newCycles = RecurringChargeCalculator.CompletedCycles(detail.BillType, fromDate, asOfDate);

                    if (newCycles <= 0) continue;

                    var periodStart = detail.LastRecurringChargeDate ?? booking.BookingDate;
                    var periodEnd = AdvanceByPeriods(detail.BillType, periodStart, newCycles);

                    detail.LastRecurringChargeDate = AdvanceByPeriods(detail.BillType, fromDate, newCycles);

                    // Recurring rent is charged on the stock still in store, not the original booked quantity
                    var stockQty = RemainingQuantity(detail);
                    if (stockQty <= 0)
                    {
                        await bookingDetailRepository.UpdateAsync(detail, cancellationToken);
                        continue;
                    }

                    var entryAmount = newCycles * stockQty * detail.BookingRate;
                    totalAmount += entryAmount;

                    var entry = new RecurringChargeEntry
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantProvider.GetTenantId(),
                        BookingId = booking.Id,
                        BookingDetailId = detail.Id,
                        RecurringChargeRunId = run.Id,
                        Source = RecurringChargeSources.Run,
                        BillPeriodFrom = periodStart,
                        BillPeriodTo = periodEnd,
                        BillType = detail.BillType,
                        Cycles = newCycles,
                        Quantity = (float)stockQty,
                        Rate = detail.BookingRate,
                        Amount = entryAmount,
                        Note = $"Recurring-charge run {run.Id:N}",
                        CreatedAt = DateTime.UtcNow,
                    };
                    await recurringChargeEntryRepository.AddAsync(entry, cancellationToken);
                    await bookingDetailRepository.UpdateAsync(detail, cancellationToken);
                    affected++;
                }
            }

            run.Status = RecurringChargeRunStatuses.Success;
            run.AffectedCount = affected;
            run.TotalRecurringChargeAmount = totalAmount;
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

    // Loads delivery lines dated up to asOfDate so stock in store can be worked out per booking line
    private Task<List<Booking>> LoadActiveBookingsAsync(DateTime asOfDate, CancellationToken ct) =>
        bookingRepository.UnfilteredQuery()
            .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.DeliveryDetails.Where(dd =>
                    !dd.IsDeleted && !dd.Delivery!.IsDeleted && dd.Delivery.DeliveryDate <= asOfDate))
            .Include(b => b.Customer)
            .Where(b => !b.IsDeleted && !b.IsArchived && b.TenantId == tenantProvider.GetTenantId())
            .ToListAsync(ct);

    /// <summary>Quantity still in store, in the booking unit.</summary>
    private static decimal RemainingQuantity(BookingDetail detail)
    {
        if (detail.BaseQuantity <= 0) return 0m;
        var deliveredBase = detail.DeliveryDetails.Sum(dd => dd.BaseQuantity);
        var ratio = Math.Max(0m, (detail.BaseQuantity - deliveredBase) / detail.BaseQuantity);
        return (decimal)detail.BookingQuantity * ratio;
    }

    private static List<RecurringChargePreviewBookingResponse> ComputePreviewItems(
        List<Booking> bookings,
        DateTime asOfDate)
    {
        var items = new List<RecurringChargePreviewBookingResponse>();

        foreach (var booking in bookings)
        {
            int lines = 0;
            decimal amount = 0m;
            DateTime? oldestFrom = null;

            foreach (var detail in booking.BookingDetails.Where(d => !d.IsDeleted))
            {
                var fromDate = detail.LastRecurringChargeDate ?? booking.BookingDate;
                int cycles = RecurringChargeCalculator.CompletedCycles(detail.BillType, fromDate, asOfDate);
                if (cycles <= 0) continue;

                var stockQty = RemainingQuantity(detail);
                if (stockQty <= 0) continue;

                lines++;
                amount += cycles * stockQty * detail.BookingRate;
                if (oldestFrom == null || fromDate < oldestFrom)
                    oldestFrom = fromDate;
            }

            if (lines == 0) continue;

            items.Add(new RecurringChargePreviewBookingResponse
            {
                BookingId = booking.Id,
                BookingNumber = booking.BookingNumber,
                CustomerName = booking.Customer?.CustomerName ?? "—",
                AffectedDetailLines = lines,
                TotalRecurringChargeAmount = amount,
                OldestLastRecurringChargeDate = oldestFrom,
            });
        }

        return items;
    }

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

    private static DateTime AdvanceByPeriods(string billType, DateTime from, int periods) =>
        billType.ToUpperInvariant() switch
        {
            BillTypes.Monthly => from.AddMonths(periods),
            BillTypes.Daily => from.AddDays(periods),
            BillTypes.Weekly => from.AddDays(periods * 7),
            BillTypes.Yearly => from.AddYears(periods),
            BillTypes.Hourly => from.AddHours(periods),
            _ => from.AddMonths(periods),
        };
}
