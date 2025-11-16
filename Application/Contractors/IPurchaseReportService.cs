using Application.Framework;

namespace Application.Contractors;

public interface IPurchaseReportService
{
    Task<PurchaseInvoiceReportResponse> GetPurchaseInvoiceAsync(long purchaseId, CancellationToken cancellationToken = default);
}
