using Application.ReponseDTO;

namespace Application.Contractors;

public interface IDatewiseActivityReportService
{
    /// <param name="fromDate">First local calendar day (inclusive).</param>
    /// <param name="toDate">Last local calendar day (inclusive).</param>
    Task<DatewiseActivityReportResponse> GetReportAsync(
        DateTime fromDate,
        DateTime toDate,
        int? customerId,
        bool includeBookings,
        bool includeDeliveries,
        CancellationToken cancellationToken = default);
}
