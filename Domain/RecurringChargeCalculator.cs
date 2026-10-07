namespace Domain;

/// <summary>
/// Pure static helpers for computing billing cycle recurring charges.
/// No DB access – safe to call from any layer.
/// </summary>
public static class RecurringChargeCalculator
{
    /// <summary>
    /// Returns the number of COMPLETE billing cycles that have elapsed
    /// between <paramref name="fromDate"/> (exclusive) and <paramref name="toDate"/> (inclusive).
    /// </summary>
    public static int CompletedCycles(string billType, DateTime fromDate, DateTime toDate)
    {
        if (toDate <= fromDate)
            return 0;

        return billType.ToUpperInvariant() switch
        {
            BillTypes.Monthly => MonthsBetween(fromDate, toDate),
            BillTypes.Daily => (int)(toDate - fromDate).TotalDays,
            BillTypes.Weekly => (int)(toDate - fromDate).TotalDays / 7,
            BillTypes.Yearly => YearsBetween(fromDate, toDate),
            BillTypes.Hourly => (int)(toDate - fromDate).TotalHours,
            _ => MonthsBetween(fromDate, toDate) // default to monthly
        };
    }

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

    // ── private helpers ──────────────────────────────────────────────────────

    // A cycle is complete only once the full period has elapsed (time-of-day included).
    // Dates are stored as UTC instants of local midnight, so comparing calendar days alone
    // would complete a cycle one local day early.
    private static int MonthsBetween(DateTime from, DateTime to)
    {
        int months = (to.Year - from.Year) * 12 + (to.Month - from.Month);
        if (months > 0 && from.AddMonths(months) > to) months--; // incomplete month
        return Math.Max(months, 0);
    }

    private static int YearsBetween(DateTime from, DateTime to)
    {
        int years = to.Year - from.Year;
        if (years > 0 && from.AddYears(years) > to) years--; // incomplete year
        return Math.Max(years, 0);
    }
}
