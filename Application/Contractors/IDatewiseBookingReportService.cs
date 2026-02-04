using Application.ReponseDTO;

namespace Application.Contractors;

public interface IDatewiseBookingReportService
{
    Task<IEnumerable<DatewiseBookingReportResponse>> GetDatewiseBookingReportAsync(
        DateTime fromDate,
        DateTime toDate,
        int? customerId = null,
        int? productId = null,
        CancellationToken cancellationToken = default);
}
