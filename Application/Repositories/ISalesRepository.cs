namespace Application.Repositories;

public interface ISalesRepository
{

    IQueryable<Sales> Query();
    Task<Sales> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<SalesResponse> ManageUpdate(SalesRequest request, Sales existingData, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}