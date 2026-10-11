namespace Application.Services.Common;

/// <summary>
/// Go-live (Company.BillingStartDate) rules shared by reports that explain ledger corrections:
/// rent months that ended before go-live and labour dated before it were settled outside the system,
/// so the ledger removes them with CORRECTION rows.
/// </summary>
public static class GoLiveRules
{
    /// <summary>Go-live date as the UTC instant of local midnight (null = no cut-off). Same as the charge ledger.</summary>
    public static async Task<DateTime?> GetGoLiveUtcAsync(IRepository<Company, int> companyRepository, CancellationToken cancellationToken)
    {
        var date = await companyRepository.Query()
            .Where(c => c.IsActive && c.BillingStartDate != null)
            .OrderBy(c => c.Id)
            .Select(c => c.BillingStartDate)
            .FirstOrDefaultAsync(cancellationToken);
        return date.HasValue ? DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Local).ToUniversalTime() : null;
    }

    /// <summary>True when rent month <paramref name="cycleNo"/> (1-based) ended before go-live.</summary>
    public static bool RentMonthBeforeGoLive(string billType, DateTime bookingDate, int cycleNo, DateTime? goLiveUtc)
        => goLiveUtc.HasValue && RecurringChargeCalculator.CycleStart(billType, bookingDate, cycleNo) < goLiveUtc.Value;

    /// <summary>
    /// True when a CORRECTION row only removes a charge that falls before go-live
    /// (rent month ended before it, or booking labour of a booking made before it).
    /// </summary>
    public static bool IsGoLiveCorrection(LedgerEntryResponse entry, string? billType, DateTime bookingDate, DateTime? goLiveUtc)
    {
        if (!goLiveUtc.HasValue || entry.Source != RecurringChargeSources.Correction) return false;
        return entry.ChargeType switch
        {
            ChargeTypes.RentCycle => billType != null && entry.CycleNo.HasValue
                                     && RentMonthBeforeGoLive(billType, bookingDate, entry.CycleNo.Value, goLiveUtc),
            ChargeTypes.BookingLabour => bookingDate < goLiveUtc.Value,
            _ => false,
        };
    }

    /// <summary>"1–3" for consecutive months, "1, 3, 5" otherwise.</summary>
    public static string MonthList(IEnumerable<int> months)
    {
        var list = months.Distinct().OrderBy(m => m).ToList();
        if (list.Count == 0) return string.Empty;
        var consecutive = list.Count > 1 && list.Last() - list.First() == list.Count - 1;
        return consecutive ? $"{list.First()}–{list.Last()}" : string.Join(", ", list);
    }
}
