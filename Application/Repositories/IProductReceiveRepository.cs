namespace Application.Repositories;

public interface IProductReceiveRepository
{
    IQueryable<Booking> Query();
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductReceiveResponse> ManageUpdate(ProductReceiveRequest request, Booking existingData, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
