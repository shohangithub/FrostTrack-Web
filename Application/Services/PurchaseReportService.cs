namespace Application.Services;

public class PurchaseReportService : IPurchaseReportService
{
    private readonly IPurchaseReportRepository _purchaseReportRepository;
    public PurchaseReportService(IPurchaseReportRepository purchaseReportRepository)
    {
        _purchaseReportRepository = purchaseReportRepository;
    }

    public Task<PurchaseInvoiceReportResponse> GetPurchaseInvoiceAsync(long purchaseId, CancellationToken cancellationToken = default)
    {
        return _purchaseReportRepository.GetPurchaseInvoiceAsync(purchaseId, cancellationToken);
    }
}

