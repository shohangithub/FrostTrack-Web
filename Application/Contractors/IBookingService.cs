using Application.Framework;

namespace Application.Contractors;

public interface IBookingService
{
    Task<IEnumerable<BookingListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<BookingListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<BookingResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BookingResponse> AddAsync(BookingRequest request, CancellationToken cancellationToken = default);
    Task<BookingResponse> UpdateAsync(Guid id, BookingRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<Guid>>> GetLookup(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default);
    Task<string> GenerateBookingNumber(CancellationToken cancellationToken = default);
    Task<BookingInvoiceWithDeliveryResponse?> GetInvoiceWithDeliveryAsync(Guid id, CancellationToken cancellationToken = default);
}
