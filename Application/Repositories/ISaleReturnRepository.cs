namespace Application.Repositories;

public interface ISaleReturnRepository
{
    IQueryable<SaleReturn> Query();
    Task<SaleReturn> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<SaleReturnResponse> ManageUpdate(SaleReturnRequest request, SaleReturn existingData, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}