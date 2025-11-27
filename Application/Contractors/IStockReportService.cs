using Application.ReponseDTO;

namespace Application.Contractors;

public interface IStockReportService
{
    Task<IEnumerable<StockReportItemResponse>> GetStockReportAsync(
        DateTime startDate,
        DateTime endDate,
        int? customerId = null,
        int? productId = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<CustomerStockReportResponse>> GetCustomerStockReportAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ProductStockReportResponse>> GetProductStockReportAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task<StockSummaryResponse> GetStockSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
