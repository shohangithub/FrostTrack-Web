using Application.ReponseDTO;
using Application.RequestDTO;

namespace Application.Contractors;

public interface IBillCollectionService
{
    Task<CustomerBalanceSummaryResponse> GetCustomerBalanceSummaryAsync(int customerId, CancellationToken cancellationToken = default);
    Task<TransactionResponse> CreateCustomerPaymentAsync(CustomerPaymentRequest request, CancellationToken cancellationToken = default);
    Task<TransactionResponse> CreateDeliveryBillCollectionAsync(DeliveryBillCollectionRequest request, CancellationToken cancellationToken = default);
    Task<TransactionResponse> CreateCustomerDiscountAsync(CustomerDiscountRequest request, CancellationToken cancellationToken = default);
    Task<List<CustomerDiscountItemResponse>> GetCustomerDiscountHistoryAsync(int customerId, CancellationToken cancellationToken = default);
    Task<List<CustomerPaymentReportItemResponse>> GetCustomerPaymentReportAsync(DateTime? startDate, DateTime? endDate, int? customerId, string? paymentMethod, CancellationToken cancellationToken = default);
    Task<List<CustomerDiscountItemResponse>> GetCustomerDiscountReportAsync(DateTime? startDate, DateTime? endDate, int? customerId, string? searchTerm, CancellationToken cancellationToken = default);
}
