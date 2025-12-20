using Application.ReponseDTO;

namespace Application.Contractors;

public interface IDailyStockBookService
{
    Task<IEnumerable<DailyStockBookItemResponse>> GetDailyStockBookAsync(
        DateTime reportDate,
        int? customerId = null,
        int? productId = null,
        CancellationToken cancellationToken = default);
}
