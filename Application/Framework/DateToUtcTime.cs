namespace Application.Framework;

/// <summary>
/// EF Core value converter to ensure DateTime is always stored and retrieved as UTC in the database
/// </summary>
public static class DateToUtcTime
{

    public static DateTime GetDateUtcTime(this DateTime date)
    {

        // Current UTC time
        var nowUtc = DateTime.UtcNow;

        // Combine report date + UTC time
        var fromUtc = new DateTime(
            date.Year,
            date.Month,
            date.Day,
            nowUtc.Hour,
            nowUtc.Minute,
            nowUtc.Second,
            nowUtc.Millisecond,
            DateTimeKind.Utc
        );

        return fromUtc;
    }
}