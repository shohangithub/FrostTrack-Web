using Application.Contractors;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class DatewiseDeliveryReportService : IDatewiseDeliveryReportService
{
    private readonly IRepository<Delivery, Guid> _deliveryRepository;
    private readonly Guid _tenantId;

    public DatewiseDeliveryReportService(
        IRepository<Delivery, Guid> deliveryRepository,
        ITenantProvider tenantProvider)
    {
        _deliveryRepository = deliveryRepository;
        _tenantId = tenantProvider.GetTenantId();
    }

    public async Task<IEnumerable<DatewiseDeliveryReportResponse>> GetDatewiseDeliveryReportAsync(
        DateTime fromDate,
        DateTime toDate,
        int? customerId = null,
        int? productId = null,
        CancellationToken cancellationToken = default)
    {
        // Set date range
        var startDate = fromDate.Date;
        var endDate = toDate.Date.AddDays(1).AddTicks(-1);

        // Build query
        var query = _deliveryRepository.Query()
            .Include(d => d.Booking)
                .ThenInclude(b => b!.Customer)
            .Include(d => d.DeliveryDetails)
                .ThenInclude(dd => dd.BookingDetail)
                    .ThenInclude(bd => bd!.Product)
            .Where(d => d.TenantId == _tenantId &&
                       d.DeliveryDate >= startDate &&
                       d.DeliveryDate <= endDate);

        // Apply customer filter
        if (customerId.HasValue)
        {
            query = query.Where(d => d.Booking!.CustomerId == customerId.Value);
        }

        var deliveries = await query
            .OrderBy(d => d.DeliveryDate)
            .ThenBy(d => d.Booking!.Customer!.CustomerName)
            .ToListAsync(cancellationToken);

        // Build report items
        var reportItems = new List<DatewiseDeliveryReportResponse>();

        foreach (var delivery in deliveries)
        {
            foreach (var detail in delivery.DeliveryDetails)
            {
                // Skip if product filter specified and doesn't match
                if (productId.HasValue && detail.BookingDetail?.ProductId != productId.Value)
                    continue;

                reportItems.Add(new DatewiseDeliveryReportResponse
                {
                    DeliveryId = delivery.Id,
                    DeliveryCode = delivery.DeliveryNumber ?? "",
                    DeliveryDate = delivery.DeliveryDate,
                    BookingCode = delivery.Booking?.BookingNumber ?? "",
                    CustomerId = delivery.Booking?.CustomerId ?? 0,
                    CustomerName = delivery.Booking?.Customer?.CustomerName ?? "",
                    CustomerMobile = delivery.Booking?.Customer?.CustomerMobile ?? "",
                    ProductId = detail.BookingDetail?.ProductId ?? 0,
                    ProductName = detail.BookingDetail?.Product?.ProductName ?? "",
                    DeliveryQuantity = detail.DeliveryQuantity,
                    DeliveryBy = "",
                    Remarks = delivery.Notes ?? ""
                });
            }
        }

        return reportItems;
    }
}
