using Application.ReponseDTO;

namespace Application.Contractors;

public interface IDatewiseDeliveryReportService
{
    Task<IEnumerable<DatewiseDeliveryReportResponse>> GetDatewiseDeliveryReportAsync(
        DateTime fromDate,
        DateTime toDate,
        int? customerId = null,
        int? productId = null,
        CancellationToken cancellationToken = default);
}
