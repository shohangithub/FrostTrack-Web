namespace Application.Repositories;

public interface IProductReceiveRepository
{
    IQueryable<ProductReceive> Query();
    Task<ProductReceive?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ProductReceiveResponse> ManageUpdate(ProductReceiveRequest request, ProductReceive existingData, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
