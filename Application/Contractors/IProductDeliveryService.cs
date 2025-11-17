using Application.Framework;
using Application.ReponseDTO;
using Application.RequestDTO;

namespace Application.Contractors;

public interface IProductDeliveryService
{
    Task<ProductDeliveryResponse> CreateAsync(ProductDeliveryRequest request);
    Task<ProductDeliveryResponse> UpdateAsync(int id, ProductDeliveryRequest request);
    Task<bool> DeleteAsync(int id);
    Task<bool> BatchDeleteAsync(int[] ids);
    Task<ProductDeliveryResponse> GetByIdAsync(int id);
    Task<PaginationResult<ProductDeliveryListResponse>> GetWithPaginationAsync(PaginationQuery query);
    Task<string> GenerateDeliveryNumberAsync();
    Task<List<CustomerStockResponse>> GetCustomerStockAsync(int customerId);
}
