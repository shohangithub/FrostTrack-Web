using Application.Framework;
using Application.ReponseDTO;

namespace Application.Contractors;

public interface IBillCollectionService
{
    Task<IEnumerable<Lookup<Guid>>> GetBookingsWithDueAsync(CancellationToken cancellationToken = default);
    Task<BookingWithDueResponse?> GetBookingForBillCollectionAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<decimal> GetBookingTotalAmountAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<decimal> GetBookingPaidAmountAsync(Guid bookingId, CancellationToken cancellationToken = default);
}
