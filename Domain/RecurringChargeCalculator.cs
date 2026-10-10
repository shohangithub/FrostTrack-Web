namespace Domain;

/// <summary>
/// Pure static helpers for computing billing cycle recurring charges.
/// No DB access – safe to call from any layer.
/// </summary>
/// <remarks>
/// Billing rule (monthly example, booking on 01-Jul):
///   cycle 1 = 01-Jul .. 01-Aug, cycle 2 = 02-Aug .. 01-Sep, cycle 3 = 02-Sep .. 01-Oct, ...
/// The anniversary day still belongs to the previous cycle; the next cycle starts the day after.
/// The same applies to weekly and yearly cycles. Daily and hourly cycles start on each new day/hour.
/// Dates are stored as UTC instants of local midnight; calendar steps are taken on the local date
/// (server time zone), comparisons are instant-based.
/// </remarks>
public static class RecurringChargeCalculator
{
    /// <summary>
    /// Start of billing cycle number <paramref name="cycleIndex"/> (0 = the first cycle, which starts on the
    /// booking/reference date itself).
    /// </summary>
    public static DateTime CycleStart(string billType, DateTime fromDate, int cycleIndex)
    {
        var fromUtc = AsUtc(fromDate);
        if (cycleIndex <= 0) return fromUtc;

        var type = (billType ?? string.Empty).ToUpperInvariant();
        if (type == BillTypes.Hourly) return fromUtc.AddHours(cycleIndex);

        // Calendar arithmetic must run on the LOCAL date: local midnight is stored as the previous
        // day's evening in UTC, so AddMonths on the UTC value would clamp/shift around month ends.
        var local = fromUtc.ToLocalTime();
        var start = type switch
        {
            BillTypes.Daily => local.AddDays(cycleIndex),
            BillTypes.Weekly => local.AddDays(7 * cycleIndex + 1),
            BillTypes.Yearly => local.AddYears(cycleIndex).AddDays(1),
            _ => local.AddMonths(cycleIndex).AddDays(1), // monthly (default)
        };
        return start.ToUniversalTime();
    }

    /// <summary>
    /// Number of billing cycles that have been COMPLETED by <paramref name="toDate"/>, counted from
    /// <paramref name="fromDate"/>. Equivalently: how many new cycles have started after the first one.
    /// The cycle running on <paramref name="toDate"/> is 1 + this value.
    /// </summary>
    public static int CompletedCycles(string billType, DateTime fromDate, DateTime toDate)
    {
        var fromUtc = AsUtc(fromDate);
        var toUtc = AsUtc(toDate);
        if (toUtc <= fromUtc)
            return 0;

        var type = (billType ?? string.Empty).ToUpperInvariant();
        var fromLocal = fromUtc.ToLocalTime();
        var toLocal = toUtc.ToLocalTime();
        int estimate = type switch
        {
            BillTypes.Daily => (int)(toUtc - fromUtc).TotalDays + 1,
            BillTypes.Hourly => (int)(toUtc - fromUtc).TotalHours,
            BillTypes.Weekly => (int)(toUtc - fromUtc).TotalDays / 7 + 1,
            BillTypes.Yearly => toLocal.Year - fromLocal.Year,
            _ => (toLocal.Year - fromLocal.Year) * 12 + (toLocal.Month - fromLocal.Month),
        };

        // Step back until the cycle start is not after toDate (at most a couple of steps)
        int cycles = Math.Max(estimate, 0);
        while (cycles > 0 && CycleStart(type, fromUtc, cycles) > toUtc)
            cycles--;
        return cycles;
    }

    /// <summary>
    /// How many cycles after the first one were already charged by recurring runs, given the booking line's
    /// LastRecurringChargeDate. New values are the start of the last charged cycle; older values are the
    /// anniversary date just before it, so a small grace step is added before counting.
    /// </summary>
    public static int ChargedCycles(string billType, DateTime bookingDate, DateTime? lastRecurringChargeDate)
    {
        if (!lastRecurringChargeDate.HasValue) return 0;
        var grace = (billType ?? string.Empty).ToUpperInvariant() == BillTypes.Hourly
            ? TimeSpan.FromMinutes(30)
            : TimeSpan.FromDays(1);
        return CompletedCycles(billType, bookingDate, AsUtc(lastRecurringChargeDate.Value).Add(grace));
    }

    /// <summary>Treats Unspecified as UTC (that is how dates are stored) and converts Local to UTC.</summary>
    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    /// <summary>
    /// Calculates the total pending (undelivered) recurring-charge amount for a set of
    /// <see cref="BookingDetail"/> items from <paramref name="fromDate"/> to
    /// <paramref name="asOfDate"/>, at 1 cycle per billing period.
    /// </summary>
    public static decimal PendingRecurringChargeAmount(
        IEnumerable<Entitites.BookingDetail> details,
        DateTime fromDate,
        DateTime asOfDate)
    {
        decimal total = 0m;
        foreach (var d in details)
        {
            int cycles = CompletedCycles(d.BillType, fromDate, asOfDate);
            if (cycles > 0)
                total += cycles * (decimal)d.BookingQuantity * d.BookingRate;
        }
        return total;
    }
}
