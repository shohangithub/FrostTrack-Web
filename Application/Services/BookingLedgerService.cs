using System.Collections.Concurrent;

namespace Application.Services;

/// <summary>
/// Keeps the charge ledger (RecurringChargeEntries) complete and correct.
///
/// Expected charges of a booking (worked out from booking + delivery data):
///   RENT_CYCLE          one per billing month that has started: stock in store at the month start × rate
///   BOOKING_LABOUR      labour on each booking line (booking date)
///   DELIVERY_LABOUR     labour on each delivery, per booking line (delivery date)
///   DELIVERY_ADJUSTMENT adjustment on each delivery (delivery date)
/// Sync compares them with what is already posted and appends only the difference:
///   never posted -> POSTED on its natural date; posted but changed -> CORRECTION dated the sync date.
/// Rows are never edited or deleted, so the ledger is the log of how every due was built.
/// </summary>
public class BookingLedgerService : IBookingLedgerService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _bookingLocks = new();
    // Local (Bangladesh) day each booking was last synced; lets read screens skip bookings already current today
    private static readonly ConcurrentDictionary<Guid, DateTime> _syncedOn = new();
    private static DateTime LocalToday => DateTime.UtcNow.AddHours(6).Date;

    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<BookingDetail, Guid> _bookingDetailRepository;
    private readonly IRepository<Delivery, Guid> _deliveryRepository;
    private readonly IRepository<RecurringChargeEntry, Guid> _entryRepository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly CurrentUser _currentUser;

    public BookingLedgerService(
        IRepository<Booking, Guid> bookingRepository,
        IRepository<BookingDetail, Guid> bookingDetailRepository,
        IRepository<Delivery, Guid> deliveryRepository,
        IRepository<RecurringChargeEntry, Guid> entryRepository,
        IRepository<Company, int> companyRepository,
        IUserContextService userContextService)
    {
        _companyRepository = companyRepository;
        _bookingRepository = bookingRepository;
        _bookingDetailRepository = bookingDetailRepository;
        _deliveryRepository = deliveryRepository;
        _entryRepository = entryRepository;
        _currentUser = userContextService.GetCurrentUser();
    }

    private DateTime? _billingStartUtc;
    private bool _billingStartLoaded;

    /// <summary>Go-live date as the UTC instant of local midnight (null = no cut-off).</summary>
    private async Task<DateTime?> GetBillingStartUtcAsync(CancellationToken cancellationToken)
    {
        if (_billingStartLoaded) return _billingStartUtc;
        var date = await _companyRepository.Query()
            .Where(c => c.IsActive && c.BillingStartDate != null)
            .OrderBy(c => c.Id)
            .Select(c => c.BillingStartDate)
            .FirstOrDefaultAsync(cancellationToken);
        _billingStartUtc = date.HasValue
            ? DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Local).ToUniversalTime()
            : null;
        _billingStartLoaded = true;
        return _billingStartUtc;
    }

    // ── sync ─────────────────────────────────────────────────────────────────

    public async Task<LedgerSyncResult> SyncBookingAsync(
        Guid bookingId, DateTime? asOf = null, Guid? runId = null, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        var result = new LedgerSyncResult();
        await SyncOneAsync(bookingId, asOf ?? DateTime.UtcNow, runId, dryRun, result, cancellationToken);
        return result;
    }

    public async Task<LedgerSyncResult> SyncBookingsAsync(IEnumerable<Guid> bookingIds, DateTime? asOf = null, CancellationToken cancellationToken = default)
    {
        var result = new LedgerSyncResult();
        var at = asOf ?? DateTime.UtcNow;
        var today = LocalToday;
        foreach (var id in bookingIds.Distinct())
        {
            // Changes (booking/delivery edits) sync the booking directly, so a booking synced today is current
            if (asOf == null && _syncedOn.TryGetValue(id, out var day) && day == today)
                continue;
            await SyncOneAsync(id, at, null, false, result, cancellationToken);
        }
        return result;
    }

    public async Task<LedgerSyncResult> SyncAllAsync(DateTime asOf, Guid? runId = null, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        var bookingIds = await _bookingRepository.Query()
            .Where(b => !b.IsDeleted && !b.IsArchived && b.BookingDate <= asOf)
            .OrderBy(b => b.BookingDate)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var result = new LedgerSyncResult();
        foreach (var id in bookingIds)
            await SyncOneAsync(id, asOf, runId, dryRun, result, cancellationToken);
        return result;
    }

    private async Task SyncOneAsync(Guid bookingId, DateTime asOf, Guid? runId, bool dryRun, LedgerSyncResult result, CancellationToken cancellationToken)
    {
        var bookingLock = _bookingLocks.GetOrAdd(bookingId, _ => new SemaphoreSlim(1, 1));
        await bookingLock.WaitAsync(cancellationToken);
        try
        {
            if (!dryRun && asOf.AddMinutes(1) >= DateTime.UtcNow)
                _syncedOn[bookingId] = LocalToday;

            var booking = await _bookingRepository.Query()
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails).ThenInclude(bd => bd.BookingUnit)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
            // Deleted / archived bookings are simply left out of dues; nothing is posted for them
            if (booking == null || booking.IsDeleted || booking.IsArchived || booking.BookingDate > asOf)
                return;

            var deliveries = await _deliveryRepository.Query()
                .Include(d => d.DeliveryDetails)
                .Where(d => d.BookingId == bookingId && d.DeliveryDate <= asOf)
                .ToListAsync(cancellationToken);

            var expected = BuildExpected(booking, deliveries, asOf, await GetBillingStartUtcAsync(cancellationToken));

            var posted = await _entryRepository.Query()
                .Where(e => e.BookingId == bookingId && e.ChargeType != ChargeTypes.Legacy
                         && e.ChargeType != ChargeTypes.ManualAdjustment) // entered by users, never calculated
                .Select(e => new { e.ChargeType, e.BookingDetailId, e.DeliveryId, e.CycleNo, e.Amount })
                .ToListAsync(cancellationToken);
            var postedByKey = posted
                .GroupBy(e => new LedgerKey(e.ChargeType, e.BookingDetailId, e.DeliveryId, e.CycleNo))
                .ToDictionary(g => g.Key, g => (Amount: g.Sum(x => x.Amount), Count: g.Count()));

            var keys = expected.Keys.Concat(postedByKey.Keys).Distinct().ToList();
            var pending = new List<RecurringChargeEntry>();
            foreach (var key in keys)
            {
                var exp = expected.GetValueOrDefault(key);
                var expAmount = exp?.Amount ?? 0m;
                var (postedAmount, postedCount) = postedByKey.GetValueOrDefault(key);
                var diff = Math.Round(expAmount - postedAmount, 2, MidpointRounding.AwayFromZero);
                if (Math.Abs(diff) < 0.005m) continue;

                var isFirstPosting = postedCount == 0;
                var entry = new RecurringChargeEntry
                {
                    Id = Guid.NewGuid(),
                    TenantId = booking.TenantId,
                    BookingId = booking.Id,
                    BookingDetailId = key.BookingDetailId,
                    DeliveryId = key.DeliveryId,
                    ChargeType = key.ChargeType,
                    CycleNo = key.CycleNo,
                    Source = isFirstPosting ? RecurringChargeSources.Posted : RecurringChargeSources.Correction,
                    EntryDate = isFirstPosting && exp != null ? exp.EntryDate : asOf,
                    BillPeriodFrom = exp?.PeriodFrom ?? asOf,
                    BillPeriodTo = exp?.PeriodTo ?? asOf,
                    BillType = exp?.BillType ?? BillTypes.Monthly,
                    Cycles = 1,
                    Quantity = exp?.Quantity ?? 0f,
                    Rate = exp?.Rate ?? 0m,
                    Amount = diff,
                    Note = isFirstPosting
                        ? exp?.Note
                        : $"Correction: was ৳{postedAmount:N2}, now ৳{expAmount:N2} (booking/delivery data changed)",
                    RecurringChargeRunId = runId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = _currentUser.Id > 0 ? _currentUser.Id : null,
                };
                pending.Add(entry);
            }

            if (pending.Count > 0)
            {
                result.BookingsAffected++;
                result.EntriesPosted += pending.Count;
                result.AmountPosted += pending.Sum(e => e.Amount);
                result.Bookings.Add(new LedgerPendingBooking
                {
                    BookingId = booking.Id,
                    BookingNumber = booking.BookingNumber,
                    CustomerName = booking.Customer?.CustomerName ?? string.Empty,
                    Entries = pending.Count,
                    Amount = pending.Sum(e => e.Amount),
                    OldestEntryDate = pending.Min(e => e.EntryDate),
                });
            }

            if (dryRun) return;

            foreach (var entry in pending.OrderBy(e => e.EntryDate))
                await _entryRepository.AddAsync(entry, cancellationToken);

            // Keep LastRecurringChargeDate (start of the latest started month) up to date
            foreach (var bd in booking.BookingDetails.Where(d => !d.IsDeleted))
            {
                var completed = RecurringChargeCalculator.CompletedCycles(bd.BillType, booking.BookingDate, asOf);
                DateTime? lastStart = completed > 0 ? RecurringChargeCalculator.CycleStart(bd.BillType, booking.BookingDate, completed) : null;
                if (bd.LastRecurringChargeDate != lastStart)
                {
                    var detailId = bd.Id;
                    await _bookingDetailRepository.DeletableQuery(x => x.Id == detailId)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastRecurringChargeDate, lastStart), cancellationToken);
                }
            }
        }
        catch
        {
            _syncedOn.TryRemove(bookingId, out _); // not current; try again next time
            throw;
        }
        finally
        {
            bookingLock.Release();
        }
    }

    private sealed record LedgerKey(string ChargeType, Guid? BookingDetailId, Guid? DeliveryId, int? CycleNo);

    private sealed record ExpectedCharge(
        decimal Amount, DateTime EntryDate, DateTime PeriodFrom, DateTime PeriodTo,
        string BillType, float Quantity, decimal Rate, string Note);

    /// <summary>Every charge the booking should have as of <paramref name="asOf"/>.</summary>
    private static Dictionary<LedgerKey, ExpectedCharge> BuildExpected(Booking booking, List<Delivery> deliveries, DateTime asOf, DateTime? billingStartUtc = null)
    {
        var expected = new Dictionary<LedgerKey, ExpectedCharge>();
        // Go-live: months that ended before it and labour dated before it were settled outside the system.
        // The month running on the go-live date is charged, dated on the go-live date.
        bool BeforeGoLive(DateTime date) => billingStartUtc.HasValue && date < billingStartUtc.Value;
        var deliveryLines = deliveries
            .SelectMany(d => d.DeliveryDetails.Where(dd => !dd.IsDeleted), (d, dd) => (Delivery: d, Line: dd))
            .ToList();

        foreach (var bd in booking.BookingDetails.Where(d => !d.IsDeleted))
        {
            var unit = bd.BookingUnit?.UnitName ?? string.Empty;
            var completed = RecurringChargeCalculator.CompletedCycles(bd.BillType, booking.BookingDate, asOf);

            for (int k = 0; k <= completed; k++)
            {
                var start = RecurringChargeCalculator.CycleStart(bd.BillType, booking.BookingDate, k);
                var next = RecurringChargeCalculator.CycleStart(bd.BillType, booking.BookingDate, k + 1);
                if (BeforeGoLive(next)) continue; // month ended before go-live
                var entryDate = BeforeGoLive(start) ? billingStartUtc!.Value : start;

                // Stock in store at the start of the month (goods delivered before that day are gone)
                var deliveredBase = deliveryLines
                    .Where(x => x.Line.BookingDetailId == bd.Id && x.Delivery.DeliveryDate < start)
                    .Sum(x => x.Line.BaseQuantity);
                var stock = bd.BaseQuantity > 0
                    ? (decimal)bd.BookingQuantity * Math.Max(0m, (bd.BaseQuantity - deliveredBase) / bd.BaseQuantity)
                    : 0m;
                var amount = Math.Round(stock * bd.BookingRate, 2, MidpointRounding.AwayFromZero);

                expected[new LedgerKey(ChargeTypes.RentCycle, bd.Id, null, k + 1)] = new ExpectedCharge(
                    amount, entryDate, start, next.AddDays(-1), bd.BillType, (float)stock, bd.BookingRate,
                    $"Month {k + 1}: {stock:0.##} {unit} × ৳{bd.BookingRate:N2}" + (entryDate != start ? " (running on go-live)" : ""));
            }

            if (bd.LabourCharge != 0 && !BeforeGoLive(booking.BookingDate))
            {
                expected[new LedgerKey(ChargeTypes.BookingLabour, bd.Id, null, null)] = new ExpectedCharge(
                    bd.LabourCharge, booking.BookingDate, booking.BookingDate, booking.BookingDate, bd.BillType,
                    bd.BookingQuantity, 0m, "Booking labour");
            }
        }

        foreach (var group in deliveryLines.GroupBy(x => (x.Delivery, x.Line.BookingDetailId)))
        {
            var delivery = group.Key.Delivery;
            var labour = group.Sum(x => x.Line.LabourCharge);
            if (labour == 0 || BeforeGoLive(delivery.DeliveryDate)) continue;
            expected[new LedgerKey(ChargeTypes.DeliveryLabour, group.Key.BookingDetailId, delivery.Id, null)] = new ExpectedCharge(
                labour, delivery.DeliveryDate, delivery.DeliveryDate, delivery.DeliveryDate, BillTypes.Monthly,
                group.Sum(x => x.Line.DeliveryQuantity), 0m, $"Delivery labour ({delivery.DeliveryNumber})");
        }

        foreach (var delivery in deliveries.Where(d => d.AdjustmentValue != 0 && !BeforeGoLive(d.DeliveryDate)))
        {
            expected[new LedgerKey(ChargeTypes.DeliveryAdjustment, null, delivery.Id, null)] = new ExpectedCharge(
                delivery.AdjustmentValue, delivery.DeliveryDate, delivery.DeliveryDate, delivery.DeliveryDate, BillTypes.Monthly,
                0f, 0m, $"Delivery adjustment ({delivery.DeliveryNumber})");
        }

        return expected;
    }

    public async Task<LedgerSyncResult> RebuildAllAsync(CancellationToken cancellationToken = default)
    {
        // Setup only: current rows are kept as LEGACY history (not counted), then everything is posted again.
        // Manual adjustments are user entries, not calculated, so they are kept as they are.
        await _entryRepository.DeletableQuery(e => e.ChargeType != ChargeTypes.Legacy && e.ChargeType != ChargeTypes.ManualAdjustment)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.ChargeType, ChargeTypes.Legacy), cancellationToken);
        _syncedOn.Clear();
        return await SyncAllAsync(DateTime.UtcNow, cancellationToken: cancellationToken);
    }

    // ── read ─────────────────────────────────────────────────────────────────

    public async Task<Dictionary<Guid, LedgerCharges>> GetChargesAsync(IEnumerable<Guid> bookingIds, DateTime asOf, CancellationToken cancellationToken = default)
    {
        var ids = bookingIds.Distinct().ToList();
        var rows = await _entryRepository.Query()
            .Where(e => ids.Contains(e.BookingId) && e.ChargeType != ChargeTypes.Legacy && e.EntryDate <= asOf)
            .GroupBy(e => new { e.BookingId, e.ChargeType, FirstMonth = e.CycleNo == 1 })
            .Select(g => new { g.Key.BookingId, g.Key.ChargeType, g.Key.FirstMonth, Amount = g.Sum(e => e.Amount) })
            .ToListAsync(cancellationToken);

        var result = ids.ToDictionary(id => id, _ => new LedgerCharges());
        foreach (var r in rows)
        {
            var c = result[r.BookingId];
            switch (r.ChargeType)
            {
                case ChargeTypes.RentCycle when r.FirstMonth: c.RentFirstMonth += r.Amount; break;
                case ChargeTypes.RentCycle: c.RentLaterMonths += r.Amount; break;
                case ChargeTypes.BookingLabour: c.BookingLabour += r.Amount; break;
                case ChargeTypes.DeliveryLabour: c.DeliveryLabour += r.Amount; break;
                case ChargeTypes.DeliveryAdjustment: c.Adjustment += r.Amount; break;
                case ChargeTypes.ManualAdjustment: c.ManualAdjustment += r.Amount; break;
            }
        }
        return result;
    }

    public async Task<List<LedgerEntryResponse>> GetEntriesAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var entries = await _entryRepository.Query()
            .Where(e => e.BookingId == bookingId && e.ChargeType != ChargeTypes.Legacy)
            .OrderBy(e => e.EntryDate).ThenBy(e => e.CycleNo).ThenBy(e => e.CreatedAt)
            .Select(e => new LedgerEntryResponse
            {
                Id = e.Id,
                EntryDate = e.EntryDate,
                ChargeType = e.ChargeType,
                Source = e.Source,
                CycleNo = e.CycleNo,
                BookingDetailId = e.BookingDetailId,
                ProductName = e.BookingDetail != null && e.BookingDetail.Product != null ? e.BookingDetail.Product.ProductName : string.Empty,
                DeliveryId = e.DeliveryId,
                BillPeriodFrom = e.BillPeriodFrom,
                BillPeriodTo = e.BillPeriodTo,
                Quantity = e.Quantity,
                Rate = e.Rate,
                Amount = e.Amount,
                Note = e.Note,
                ReferenceCode = e.ReferenceCode,
                ReversalOfId = e.ReversalOfId,
                CreatedAt = e.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        // Delivery numbers (deliveries may be deleted later, so look past the soft-delete filter)
        var deliveryIds = entries.Where(e => e.DeliveryId.HasValue).Select(e => e.DeliveryId!.Value).Distinct().ToList();
        if (deliveryIds.Count > 0)
        {
            var numbers = await _deliveryRepository.UnfilteredQuery()
                .Where(d => deliveryIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.DeliveryNumber, cancellationToken);
            foreach (var e in entries.Where(e => e.DeliveryId.HasValue))
                e.DeliveryNumber = numbers.GetValueOrDefault(e.DeliveryId!.Value);
        }
        return entries;
    }

    // ── manual adjustments ───────────────────────────────────────────────────

    public async Task<LedgerEntryResponse> AddManualAdjustmentAsync(
        Guid bookingId, decimal amount, DateTime entryDate, string reason, string? referenceCode, CancellationToken cancellationToken = default)
    {
        if (amount <= 0) throw new Exception("Adjustment amount must be greater than zero");
        if (string.IsNullOrWhiteSpace(reason)) throw new Exception("A reason is required for an adjustment");

        var booking = await _bookingRepository.Query()
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking == null || booking.IsDeleted) throw new Exception("Booking not found");
        if (booking.IsArchived) throw new Exception("This booking is archived; its balance was carried forward to the customer");

        var entry = new RecurringChargeEntry
        {
            Id = Guid.NewGuid(),
            TenantId = booking.TenantId,
            BookingId = booking.Id,
            ChargeType = ChargeTypes.ManualAdjustment,
            Source = RecurringChargeSources.Manual,
            EntryDate = entryDate,
            BillPeriodFrom = entryDate,
            BillPeriodTo = entryDate,
            BillType = BillTypes.Monthly,
            Cycles = 1,
            Amount = Math.Round(amount, 2),
            Note = Truncate(reason.Trim(), 500),
            ReferenceCode = referenceCode,
            CreatedAt = DateTime.UtcNow,
            CreatedById = _currentUser.Id > 0 ? _currentUser.Id : null,
        };
        await _entryRepository.AddAsync(entry, cancellationToken);
        return (await GetEntriesAsync(bookingId, cancellationToken)).First(e => e.Id == entry.Id);
    }

    public async Task<bool> ReverseManualAdjustmentAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        var original = await _entryRepository.Query()
            .FirstOrDefaultAsync(e => e.Id == entryId, cancellationToken);
        if (original == null || original.ChargeType != ChargeTypes.ManualAdjustment || original.Source != RecurringChargeSources.Manual)
            throw new Exception("Adjustment not found");
        if (await _entryRepository.Query().AnyAsync(e => e.ReversalOfId == entryId, cancellationToken))
            throw new Exception("This adjustment is already cancelled");

        // The ledger is append-only: cancelling posts the opposite amount, dated today
        var now = DateTime.UtcNow;
        await _entryRepository.AddAsync(new RecurringChargeEntry
        {
            Id = Guid.NewGuid(),
            TenantId = original.TenantId,
            BookingId = original.BookingId,
            ChargeType = ChargeTypes.ManualAdjustment,
            Source = RecurringChargeSources.Reversal,
            EntryDate = now,
            BillPeriodFrom = now,
            BillPeriodTo = now,
            BillType = BillTypes.Monthly,
            Cycles = 1,
            Amount = -original.Amount,
            Note = Truncate($"Cancelled: {original.Note}", 500),
            ReferenceCode = original.ReferenceCode,
            ReversalOfId = original.Id,
            CreatedAt = now,
            CreatedById = _currentUser.Id > 0 ? _currentUser.Id : null,
        }, cancellationToken);
        return true;
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    public async Task<List<LedgerCheckItem>> CheckAsync(bool onlyDifferences = true, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var bookings = await _bookingRepository.Query()
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails)
            .Where(b => !b.IsDeleted && !b.IsArchived && b.BookingDate <= now)
            .OrderBy(b => b.BookingDate)
            .ToListAsync(cancellationToken);
        var bookingIds = bookings.Select(b => b.Id).ToList();

        var deliveries = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails)
            .Where(d => bookingIds.Contains(d.BookingId))
            .ToListAsync(cancellationToken);
        var ledger = await GetChargesAsync(bookingIds, now, cancellationToken);
        var pending = (await SyncAllAsync(now, null, dryRun: true, cancellationToken)).Bookings
            .ToDictionary(b => b.BookingId, b => b.Amount);

        var billingStartUtc = await GetBillingStartUtcAsync(cancellationToken);
        var items = new List<LedgerCheckItem>();
        foreach (var booking in bookings)
        {
            // Bookings from before go-live can't be compared with the full-history calculation;
            // for them the check only reports postings that are still pending.
            var comparable = !billingStartUtc.HasValue || booking.BookingDate >= billingStartUtc.Value;
            // Independent calculation (delivery rent as saved on the delivery)
            var (_, _, calculated, _) = BookingDueCalculator.CalculateBookingAccruedDetails(
                booking, booking.BookingDetails.Where(d => !d.IsDeleted), deliveries.Where(d => d.BookingId == booking.Id), now);
            // Manual adjustments are not part of the independent calculation
            var charges = ledger.GetValueOrDefault(booking.Id);
            var ledgerTotal = (charges?.Total ?? 0m) - (charges?.ManualAdjustment ?? 0m);
            var item = new LedgerCheckItem
            {
                BookingId = booking.Id,
                BookingNumber = booking.BookingNumber,
                BookingDate = booking.BookingDate,
                CustomerName = booking.Customer?.CustomerName ?? string.Empty,
                LedgerTotal = ledgerTotal,
                CalculatedTotal = comparable ? calculated : null,
                Difference = comparable ? ledgerTotal - calculated : 0m,
                PendingSync = pending.GetValueOrDefault(booking.Id),
            };
            if (!onlyDifferences || Math.Abs(item.Difference) >= 0.01m || Math.Abs(item.PendingSync) >= 0.01m)
                items.Add(item);
        }
        return items;
    }
}
