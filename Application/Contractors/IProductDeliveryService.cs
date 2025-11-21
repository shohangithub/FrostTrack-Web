using Application.Framework;
using Application.ReponseDTO;
using Application.RequestDTO;

namespace Application.Contractors;

public interface IDeliveryService
{
    Task<DeliveryResponse> CreateAsync(CreateDeliveryRequest request, CancellationToken cancellationToken = default);
    Task<DeliveryResponse> UpdateAsync(Guid id, UpdateDeliveryRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> BatchDeleteAsync(Guid[] ids);
    Task<DeliveryResponse> GetByIdAsync(Guid id);
    Task<PaginationResult<DeliveryResponse>> GetWithPaginationAsync(PaginationQuery query);
    Task<string> GenerateDeliveryNumberAsync();
    Task<BookingForDeliveryResponse> GetBookingForDeliveryAsync(string bookingNumber);
    Task<List<RemainingQuantityResponse>> GetRemainingQuantitiesAsync(Guid bookingId);
    Task<IEnumerable<Lookup<Guid>>> GetBookingLookupAsync();
    Task<decimal> GetBookingPreviousPaymentsAsync(Guid bookingId);
    Task<IEnumerable<Lookup<Guid>>> GetDeliveryLookupAsync();
    Task<DeliveryInvoiceResponse> GetInvoiceByIdAsync(Guid id);
}
