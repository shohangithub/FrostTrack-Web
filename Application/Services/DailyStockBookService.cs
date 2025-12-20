using Application.Contractors;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class DailyStockBookService : IDailyStockBookService
{
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<Delivery, Guid> _deliveryRepository;
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly Guid _tenantId;

    public DailyStockBookService(
        IRepository<Booking, Guid> bookingRepository,
        IRepository<Delivery, Guid> deliveryRepository,
        IRepository<Transaction, Guid> transactionRepository,
        ITenantProvider tenantProvider)
    {
        _bookingRepository = bookingRepository;
        _deliveryRepository = deliveryRepository;
        _transactionRepository = transactionRepository;
        _tenantId = tenantProvider.GetTenantId();
    }

    public async Task<IEnumerable<DailyStockBookItemResponse>> GetDailyStockBookAsync(
        DateTime reportDate,
        int? customerId = null,
        int? productId = null,
        CancellationToken cancellationToken = default)
    {
        // Set date range for the report date
        var startOfDay = reportDate.Date;
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        // Get all bookings up to the report date (to calculate previous stock)
        var allBookingsQuery = _bookingRepository.Query()
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.Product)
            .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.BookingUnit)
            .Where(b => b.TenantId == _tenantId && b.BookingDate <= endOfDay);

        // Apply filters
        if (customerId.HasValue)
        {
            allBookingsQuery = allBookingsQuery.Where(b => b.CustomerId == customerId.Value);
        }

        var allBookings = await allBookingsQuery.ToListAsync(cancellationToken);

        // Get all deliveries up to the report date
        var bookingIds = allBookings.Select(b => b.Id).ToList();
        var allDeliveries = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails)
                .ThenInclude(dd => dd.BookingDetail)
            .Where(d => bookingIds.Contains(d.BookingId) && 
                       d.TenantId == _tenantId && 
                       d.DeliveryDate <= endOfDay)
            .ToListAsync(cancellationToken);

        // Get bill collections (transactions) for the report date
        var billCollections = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Include(t => t.Booking)
            .Where(t => t.TenantId == _tenantId &&
                       t.TransactionDate >= startOfDay &&
                       t.TransactionDate <= endOfDay &&
                       t.TransactionHead!.UsageFor == UsageFor.BILL_COLLECTION &&
                       t.BookingId != null)
            .ToListAsync(cancellationToken);

        // Build daily stock book items
        var stockBookItems = new List<DailyStockBookItemResponse>();

        // Group by customer and product
        var customerProductGroups = allBookings
            .SelectMany(b => b.BookingDetails.Select(bd => new
            {
                CustomerId = b.CustomerId,
                CustomerName = b.Customer?.CustomerName ?? "",
                ProductId = bd.ProductId,
                ProductName = bd.Product?.ProductName ?? "",
                BookingDetail = bd,
                Booking = b
            }))
            .GroupBy(x => new { x.CustomerId, x.CustomerName, x.ProductId, x.ProductName });

        foreach (var group in customerProductGroups)
        {
            // Skip if product filter specified and doesn't match
            if (productId.HasValue && group.Key.ProductId != productId.Value)
                continue;

            var bookingDetails = group.Select(x => x.BookingDetail).ToList();
            var _bookingIds = group.Select(x => x.Booking.Id).Distinct().ToList();

            // Calculate previous stock (bookings before report date)
            var previousBookings = bookingDetails
                .Where(bd => bd.Booking!.BookingDate < startOfDay)
                .Sum(bd => bd.BookingQuantity);

            var previousDeliveries = allDeliveries
                .SelectMany(d => d.DeliveryDetails)
                .Where(dd => bookingDetails.Select(bd => bd.Id).Contains(dd.BookingDetailId) &&
                           dd.Delivery!.DeliveryDate < startOfDay)
                .Sum(dd => dd.DeliveryQuantity);

            var previousStock = previousBookings - previousDeliveries;

            // Calculate today's bookings
            var todayBookings = bookingDetails
                .Where(bd => bd.Booking!.BookingDate >= startOfDay && 
                           bd.Booking.BookingDate <= endOfDay)
                .Sum(bd => bd.BookingQuantity);

            // Calculate today's deliveries
            var todayDeliveries = allDeliveries
                .SelectMany(d => d.DeliveryDetails)
                .Where(dd => bookingDetails.Select(bd => bd.Id).Contains(dd.BookingDetailId) &&
                           dd.Delivery!.DeliveryDate >= startOfDay &&
                           dd.Delivery.DeliveryDate <= endOfDay)
                .Sum(dd => dd.DeliveryQuantity);

            // Current stock = previous stock + today's bookings - today's deliveries
            var currentStock = previousStock + todayBookings - todayDeliveries;

            // Get receipt numbers from bill collections for this customer's bookings on this date
            var receiptNumbers = billCollections
                .Where(t => _bookingIds.Contains(t.BookingId!.Value))
                .Select(t => t.TransactionCode)
                .Distinct()
                .ToList();

            var receiptNo = receiptNumbers.Any() ? string.Join(", ", receiptNumbers) : "-";

            // Calculate received rent (from bill collections)
            var receivedRent = billCollections
                .Where(t => _bookingIds.Contains(t.BookingId!.Value))
                .Sum(t => Math.Abs(t.NetAmount));

            stockBookItems.Add(new DailyStockBookItemResponse
            {
                CustomerId = group.Key.CustomerId,
                CustomerName = group.Key.CustomerName,
                ProductId = group.Key.ProductId,
                ProductName = group.Key.ProductName,
                PreviousStock = previousStock,
                TotalBooking = todayBookings,
                TotalDelivery = todayDeliveries,
                ReceiptNo = receiptNo,
                CurrentStock = currentStock,
                ReceivedRent = receivedRent
            });
        }

        return stockBookItems.OrderBy(x => x.CustomerName).ThenBy(x => x.ProductName);
    }
}
