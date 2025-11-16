namespace Application.Repositories;

public interface IPurchaseRepository
{

    IQueryable<Purchase> Query();
    Task<Purchase> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<PurchaseResponse> ManageUpdate(PurchaseRequest request, Purchase existingData, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}