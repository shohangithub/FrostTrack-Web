using Application.Framework;
using Application.ReponseDTO;
using Application.RequestDTO;

namespace Application.Contractors;

public interface IDeliveryService
{
    Task<DeliveryResponse> CreateAsync(DeliveryRequest request);
    Task<DeliveryResponse> UpdateAsync(Guid id, DeliveryRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> BatchDeleteAsync(Guid[] ids);
    Task<DeliveryResponse> GetByIdAsync(Guid id);
    Task<PaginationResult<DeliveryListResponse>> GetWithPaginationAsync(PaginationQuery query);
    Task<string> GenerateDeliveryNumberAsync();
    Task<List<CustomerStockResponse>> GetCustomerStockAsync(int customerId);
}
