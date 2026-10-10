namespace Application.Services.Common;

/// <summary>
/// Converts a local calendar date (or date range) into the UTC query window used by
/// the financial reports (Cash Book, Bank Book, General Ledger, Trial Balance).
/// Single-day reports use <see cref="ForDay"/>, which is simply a range where From == To,
/// so daily and date-range reports always share the exact same calculation path.
/// </summary>
public readonly record struct ReportDateWindow(
    DateTime FromDate,
    DateTime ToDate,
    DateTime FromUtc,
    DateTime ToUtc,
    DateTime OpeningAnchorUtc,
    DateTime ClosingAnchorUtc)
{
    /// <summary>Maximum number of calendar days allowed in a single range report.</summary>
    public const int MaxRangeDays = 366;

    public int TotalDays => (int)(ToDate - FromDate).TotalDays + 1;

    public static ReportDateWindow ForDay(DateTime reportDate) => Create(reportDate.Date, reportDate.Date);

    public static ReportDateWindow ForRange(DateTime fromDate, DateTime toDate)
    {
        var from = fromDate.Date;
        var to = toDate.Date;

        if (from > to)
        {
            throw new ArgumentException("From date cannot be after To date.");
        }

        if ((to - from).TotalDays + 1 > MaxRangeDays)
        {
            throw new ArgumentException($"Date range cannot exceed {MaxRangeDays} days.");
        }

        return Create(from, to);
    }

    /// <summary>Start of the given local calendar day, expressed in UTC.</summary>
    public static DateTime DayStartUtc(DateTime localDay) =>
        DateTime.SpecifyKind(localDay.Date, DateTimeKind.Local).ToUniversalTime();

    /// <summary>
    /// Cut-off used by <see cref="IBalanceCalculatorService"/> to locate the latest explicit
    /// opening-balance entry for a day. Mirrors the original single-day report logic exactly.
    /// </summary>
    public static DateTime OpeningAnchorFor(DateTime localDay)
    {
        var endOfDayUtc = DayStartUtc(localDay.Date.AddDays(1));
        var dateWithUtcTime = localDay.Date.GetDateUtcTime();
        return endOfDayUtc > dateWithUtcTime ? endOfDayUtc : dateWithUtcTime;
    }

    /// <summary>Converts a stored UTC timestamp to its local calendar day.</summary>
    public static DateTime ToLocalDay(DateTime utc) => utc.Kind == DateTimeKind.Local
        ? utc.Date
        : DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().Date;

    private static ReportDateWindow Create(DateTime from, DateTime to) => new(
        from,
        to,
        DayStartUtc(from),
        DayStartUtc(to.AddDays(1)),
        OpeningAnchorFor(from),
        OpeningAnchorFor(to));
}
