using Application.ReponseDTO;
using Application.RequestDTO;

namespace Application.Contractors;

public interface IBillCollectionService
{
    Task<CustomerBalanceSummaryResponse> GetCustomerBalanceSummaryAsync(int customerId, CancellationToken cancellationToken = default);
    Task<TransactionResponse> CreateCustomerPaymentAsync(CustomerPaymentRequest request, CancellationToken cancellationToken = default);

    // Booking-wise payment
    Task<CustomerDueBookingsResponse> GetCustomerDueBookingsAsync(int customerId, bool includeAll, CancellationToken cancellationToken = default);
    Task<BookingPaymentDetailResponse> GetBookingPaymentDetailAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<BookingPaymentResultResponse> CreateBookingPaymentAsync(BookingPaymentRequest request, CancellationToken cancellationToken = default);
    Task<CustomerAdvancesResponse> GetCustomerAdvancesAsync(int customerId, CancellationToken cancellationToken = default);
    Task<bool> RemoveAdvanceAllocationAsync(Guid allocationId, CancellationToken cancellationToken = default);
    Task<PaymentReceiptInfoResponse> GetPaymentReceiptInfoAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<TransactionResponse> CreateDeliveryBillCollectionAsync(DeliveryBillCollectionRequest request, CancellationToken cancellationToken = default);
    Task<TransactionResponse> CreateCustomerDiscountAsync(CustomerDiscountRequest request, CancellationToken cancellationToken = default);
    Task<List<CustomerDiscountItemResponse>> GetCustomerDiscountHistoryAsync(int customerId, CancellationToken cancellationToken = default);
    Task<List<CustomerPaymentReportItemResponse>> GetCustomerPaymentReportAsync(DateTime? startDate, DateTime? endDate, int? customerId, string? paymentMethod, CancellationToken cancellationToken = default);
    Task<List<BookingAdjustmentLine>> GetAdjustmentHistoryAsync(int customerId, Guid? bookingId, CancellationToken cancellationToken = default);
    Task<BookingAdjustmentResultResponse> CreateAdjustmentAsync(BookingAdjustmentRequest request, CancellationToken cancellationToken = default);
    Task<bool> CancelAdjustmentAsync(string source, Guid id, CancellationToken cancellationToken = default);
    Task<List<CustomerDiscountItemResponse>> GetCustomerDiscountReportAsync(DateTime? startDate, DateTime? endDate, int? customerId, string? searchTerm, CancellationToken cancellationToken = default);
}
