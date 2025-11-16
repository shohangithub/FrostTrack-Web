namespace Application.Repositories;

public interface IPurchaseReportRepository
{
    Task<PurchaseInvoiceReportResponse> GetPurchaseInvoiceAsync(long purchaseId, CancellationToken cancellationToken = default);
}