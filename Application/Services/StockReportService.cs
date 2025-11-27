using Application.Contractors;
using Application.Contractors.Authentication;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class StockReportService : IStockReportService
{
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<BookingDetail, Guid> _bookingDetailRepository;
    private readonly IRepository<Delivery, Guid> _deliveryRepository;
    private readonly IRepository<DeliveryDetail, Guid> _deliveryDetailRepository;
    private readonly Guid _tenantId;

    public StockReportService(
        IRepository<Booking, Guid> bookingRepository,
        IRepository<BookingDetail, Guid> bookingDetailRepository,
        IRepository<Delivery, Guid> deliveryRepository,
        IRepository<DeliveryDetail, Guid> deliveryDetailRepository,
        ITenantProvider tenantProvider)
    {
        _bookingRepository = bookingRepository;
        _bookingDetailRepository = bookingDetailRepository;
        _deliveryRepository = deliveryRepository;
        _deliveryDetailRepository = deliveryDetailRepository;
        _tenantId = tenantProvider.GetTenantId();
    }

    public async Task<IEnumerable<StockReportItemResponse>> GetStockReportAsync(
        DateTime startDate,
        DateTime endDate,
        int? customerId = null,
        int? productId = null,
        CancellationToken cancellationToken = default)
    {
        // Get bookings with details in date range
        var query = _bookingRepository.Query()
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.Product)
            .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.BookingUnit)
            .Where(b => b.TenantId == _tenantId &&
                       b.BookingDate >= startDate &&
                       b.BookingDate <= endDate);

        // Apply filters
        if (customerId.HasValue)
        {
            query = query.Where(b => b.CustomerId == customerId.Value);
        }

        var bookings = await query.ToListAsync(cancellationToken);

        // Get all deliveries for these bookings
        var bookingIds = bookings.Select(b => b.Id).ToList();
        var deliveries = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails)
            .Where(d => bookingIds.Contains(d.BookingId) && d.TenantId == _tenantId)
            .ToListAsync(cancellationToken);

        // Build stock report items
        var stockItems = new List<StockReportItemResponse>();

        foreach (var booking in bookings)
        {
            foreach (var bookingDetail in booking.BookingDetails)
            {
                // Skip if product filter specified and doesn't match
                if (productId.HasValue && bookingDetail.ProductId != productId.Value)
                    continue;

                // Calculate total delivered for this booking detail
                var deliveredQuantity = deliveries
                    .SelectMany(d => d.DeliveryDetails)
                    .Where(dd => dd.BookingDetailId == bookingDetail.Id)
                    .Sum(dd => dd.DeliveryQuantity);

                var remainingQuantity = bookingDetail.BookingQuantity - deliveredQuantity;

                // Determine status
                string status;
                if (deliveredQuantity == 0)
                    status = "Pending";
                else if (deliveredQuantity < bookingDetail.BookingQuantity)
                    status = "Partial";
                else
                    status = "Completed";

                // Get last delivery date for this booking detail
                var lastDeliveryDate = deliveries
                    .SelectMany(d => d.DeliveryDetails)
                    .Where(dd => dd.BookingDetailId == bookingDetail.Id)
                    .Select(dd => dd.Delivery?.DeliveryDate)
                    .OrderByDescending(d => d)
                    .FirstOrDefault();

                // Calculate total value
                var totalValue = bookingDetail.BookingRate * (decimal)bookingDetail.BookingQuantity;

                stockItems.Add(new StockReportItemResponse
                {
                    BookingId = booking.Id,
                    BookingNumber = booking.BookingNumber,
                    BookingDate = booking.BookingDate,
                    CustomerId = booking.CustomerId,
                    CustomerName = booking.Customer?.CustomerName ?? "",
                    ProductId = bookingDetail.ProductId,
                    ProductName = bookingDetail.Product?.ProductName ?? "",
                    BookingQuantity = bookingDetail.BookingQuantity,
                    DeliveredQuantity = deliveredQuantity,
                    RemainingQuantity = remainingQuantity,
                    UnitName = bookingDetail.BookingUnit?.UnitName ?? "",
                    BookingRate = bookingDetail.BookingRate,
                    TotalValue = totalValue,
                    LastDeliveryDate = lastDeliveryDate,
                    Status = status
                });
            }
        }

        return stockItems.OrderBy(s => s.BookingDate).ThenBy(s => s.BookingNumber);
    }

    public async Task<IEnumerable<CustomerStockReportResponse>> GetCustomerStockReportAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var stockItems = await GetStockReportAsync(startDate, endDate, null, null, cancellationToken);

        // Group by customer
        var groupedByCustomer = stockItems
            .GroupBy(s => new { s.CustomerId, s.CustomerName })
            .Select(g => new CustomerStockReportResponse
            {
                CustomerId = g.Key.CustomerId,
                CustomerName = g.Key.CustomerName,
                Items = g.ToList(),
                Summary = new CustomerStockSummary
                {
                    TotalBookedQuantity = g.Sum(i => i.BookingQuantity),
                    TotalDeliveredQuantity = g.Sum(i => i.DeliveredQuantity),
                    TotalRemainingQuantity = g.Sum(i => i.RemainingQuantity),
                    TotalValue = g.Sum(i => i.TotalValue)
                }
            })
            .OrderBy(c => c.CustomerName)
            .ToList();

        return groupedByCustomer;
    }

    public async Task<IEnumerable<ProductStockReportResponse>> GetProductStockReportAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var stockItems = await GetStockReportAsync(startDate, endDate, null, null, cancellationToken);

        // Group by product
        var groupedByProduct = stockItems
            .GroupBy(s => new { s.ProductId, s.ProductName })
            .Select(g => new ProductStockReportResponse
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                Items = g.ToList(),
                Summary = new ProductStockSummary
                {
                    TotalBookedQuantity = g.Sum(i => i.BookingQuantity),
                    TotalDeliveredQuantity = g.Sum(i => i.DeliveredQuantity),
                    TotalRemainingQuantity = g.Sum(i => i.RemainingQuantity),
                    TotalValue = g.Sum(i => i.TotalValue)
                }
            })
            .OrderBy(p => p.ProductName)
            .ToList();

        return groupedByProduct;
    }

    public async Task<StockSummaryResponse> GetStockSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var stockItems = await GetStockReportAsync(startDate, endDate, null, null, cancellationToken);
        var itemsList = stockItems.ToList();

        return new StockSummaryResponse
        {
            TotalBookings = itemsList.Select(i => i.BookingId).Distinct().Count(),
            TotalProducts = itemsList.Select(i => i.ProductId).Distinct().Count(),
            TotalBookedQuantity = itemsList.Sum(i => i.BookingQuantity),
            TotalDeliveredQuantity = itemsList.Sum(i => i.DeliveredQuantity),
            TotalRemainingQuantity = itemsList.Sum(i => i.RemainingQuantity),
            TotalValue = itemsList.Sum(i => i.TotalValue)
        };
    }
}
