namespace Application.Repositories;

public interface IProductReceiveRepository
{
    IQueryable<Booking> Query();
    Task<Booking?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ProductReceiveResponse> ManageUpdate(ProductReceiveRequest request, Booking existingData, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
