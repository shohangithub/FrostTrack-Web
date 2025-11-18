namespace Application.Repositories;

public interface IStockRepository
{
    Task<bool> ManageAddPurchaseStock(Purchase entity, CancellationToken cancellationToken);
    Task<bool> ManageAddSalesStock(Sales request, CancellationToken cancellationToken);
    Task<bool> ManageAddSaleReturnStock(SaleReturn entity, CancellationToken cancellationToken);
    Task<bool> ManageDeleteSaleReturnStock(SaleReturn entity, CancellationToken cancellationToken);
    Task<bool> ManageBatchDeleteSaleReturnStock(List<SaleReturn> entities, CancellationToken cancellationToken);
    Task<bool> ManageAddDamageStock(Damage entity, CancellationToken cancellationToken);
    Task<bool> ManageDeleteDamageStock(Damage entity, CancellationToken cancellationToken);
    Task<bool> ManageBatchDeleteDamageStock(List<Damage> entities, CancellationToken cancellationToken);
    Task<bool> ManageAddProductReceiveStock(Booking entity, CancellationToken cancellationToken);
}
