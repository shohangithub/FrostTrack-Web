namespace Application.ReponseDTO;

public record DashboardStatsResponse(
    int TotalBookings,
    decimal TotalBookingAmount,
    int TotalDeliveries,
    decimal TotalDeliveryAmount,
    int TotalBillCollections,
    decimal TotalBillCollectionAmount,
    decimal TotalRevenue,
    decimal TotalExpense,
    decimal NetRevenue,
    DateTime StartDate,
    DateTime EndDate,
    int PeriodDays
);

public record DashboardCardData(
    string Title,
    string Value,
    string SubValue,
    int ProgressPercentage,
    string ProgressType // success, warning, info, danger
);

public record DashboardTrendsResponse(
    List<DailyTrendData> RevenueTrend,
    List<DailyTrendData> ExpenseTrend,
    List<DailyTrendData> NetProfitTrend,
    List<DailyTrendData> BookingTrend,
    List<DailyTrendData> DeliveryTrend,
    Dictionary<string, List<decimal>> TransactionCategoryTrends,
    List<string> DateLabels
);

public record DailyTrendData(
    DateTime Date,
    decimal Amount,
    int Count
);
