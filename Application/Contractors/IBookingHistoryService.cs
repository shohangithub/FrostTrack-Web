using Application.ReponseDTO;

namespace Application.Contractors;

public interface IBookingHistoryService
{
    Task<BookingHistoryResponse> GetHistoryAsync(Guid bookingId, CancellationToken cancellationToken = default);
}
