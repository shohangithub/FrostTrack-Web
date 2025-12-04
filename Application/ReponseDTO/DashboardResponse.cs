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
