namespace Application.Repositories;

public interface IBookingRepository
{
    IQueryable<Booking> Query();
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BookingResponse> ManageUpdate(BookingRequest request, Booking existingData, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
