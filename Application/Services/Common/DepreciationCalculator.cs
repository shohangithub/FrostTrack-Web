namespace Application.Services.Common;

public static class FiscalYear
{
    /// <summary>First day of the fiscal year that contains <paramref name="day"/>.</summary>
    public static DateTime StartOf(DateTime day, int startMonth)
    {
        var start = new DateTime(day.Year, startMonth, 1);
        return start > day.Date ? start.AddYears(-1) : start;
    }
}

/// <summary>
/// Written down value (reducing balance, like the audited fixed asset schedule): each fiscal year the
/// asset loses Rate% of its value at the start of that year; a part year is charged by days.
/// </summary>
public static class DepreciationCalculator
{
    /// <summary>Value at the end of local calendar day <paramref name="asOfDay"/>.</summary>
    public static decimal GetWdv(Asset asset, DateTime asOfDay, DateTime? balanceSheetStartDay, int fiscalYearStartMonth)
    {
        var (fromDay, value) = GetBase(asset, balanceSheetStartDay);
        var toDay = asOfDay.Date.AddDays(1); // exclusive
        if (toDay <= fromDay || asset.DepreciationRate <= 0) return value;

        var rate = asset.DepreciationRate / 100m;
        var current = fromDay;
        while (current < toDay)
        {
            var yearEnd = FiscalYear.StartOf(current, fiscalYearStartMonth).AddYears(1);
            var segmentEnd = yearEnd < toDay ? yearEnd : toDay;
            var yearDays = (decimal)(yearEnd - yearEnd.AddYears(-1)).TotalDays;
            value -= value * rate * (decimal)(segmentEnd - current).TotalDays / yearDays;
            current = segmentEnd;
        }
        return Math.Round(value, 2);
    }

    /// <summary>Day depreciation starts from and the value on it.</summary>
    private static (DateTime FromDay, decimal Value) GetBase(Asset asset, DateTime? startDay)
    {
        var purchaseDay = asset.PurchaseDate.HasValue ? ReportDateWindow.ToLocalDay(asset.PurchaseDate.Value) : (DateTime?)null;

        // Bought before the balance sheet start: continue from the audited written down value
        if (asset.OpeningWdv.HasValue && startDay.HasValue && (purchaseDay == null || purchaseDay <= startDay))
            return (startDay.Value.Date.AddDays(1), asset.OpeningWdv.Value);

        var cost = asset.PurchaseCost > 0 ? asset.PurchaseCost : asset.CurrentValue;
        var fromDay = purchaseDay ?? startDay?.Date.AddDays(1) ?? DateTime.Today;
        return (fromDay, cost);
    }
}
