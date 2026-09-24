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
            .Where(b => b.TenantId == _tenantId && !b.IsArchived && b.BookingDate <= endOfDay);

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
                       t.TransactionHead!.UsageFor == UsageFor.CUSTOMER_PAYMENT &&
                       t.BookingId != null)
            .ToListAsync(cancellationToken);

        // Build daily stock book items per single booking
        var stockBookItems = new List<DailyStockBookItemResponse>();

        foreach (var booking in allBookings)
        {
            var details = productId.HasValue
                ? booking.BookingDetails.Where(bd => bd.ProductId == productId.Value).ToList()
                : booking.BookingDetails.ToList();

            if (!details.Any())
                continue;

            var detailIds = details.Select(d => d.Id).ToHashSet();
            var productName = string.Join(", ", details.Select(d => d.Product?.ProductName).Where(n => !string.IsNullOrEmpty(n)).Distinct());
            var totalBookedQuantity = details.Sum(d => d.BookingQuantity);

            // Calculate previous stock (stock before report date)
            var previousBookings = booking.BookingDate < startOfDay
                ? totalBookedQuantity
                : 0;

            var previousDeliveries = allDeliveries
                .Where(d => d.BookingId == booking.Id && d.DeliveryDate < startOfDay)
                .SelectMany(d => d.DeliveryDetails)
                .Where(dd => detailIds.Contains(dd.BookingDetailId))
                .Sum(dd => dd.DeliveryQuantity);

            var previousStock = Math.Max(0, previousBookings - previousDeliveries);

            // Calculate today's bookings
            var todayBookings = (booking.BookingDate >= startOfDay && booking.BookingDate <= endOfDay)
                ? totalBookedQuantity
                : 0;

            // Calculate today's deliveries
            var todayDeliveries = allDeliveries
                .Where(d => d.BookingId == booking.Id && 
                            d.DeliveryDate >= startOfDay && 
                            d.DeliveryDate <= endOfDay)
                .SelectMany(d => d.DeliveryDetails)
                .Where(dd => detailIds.Contains(dd.BookingDetailId))
                .Sum(dd => dd.DeliveryQuantity);

            // Current stock = previous stock + today's bookings - today's deliveries
            var currentStock = Math.Max(0, previousStock + todayBookings - todayDeliveries);

            // Skip records where current stock is zero or less and no activity occurred on the report date
            if (currentStock <= 0 && todayBookings <= 0 && todayDeliveries <= 0)
                continue;

            var receiptNo = !string.IsNullOrWhiteSpace(booking.BookingNumber)
                ? booking.BookingNumber.Trim()
                : "-";

            // Calculate received rent for this booking on the report date
            var receivedRent = billCollections
                .Where(t => t.BookingId == booking.Id)
                .Sum(t => Math.Abs(t.NetAmount));

            stockBookItems.Add(new DailyStockBookItemResponse
            {
                BookingId = booking.Id,
                BookingDate = booking.BookingDate,
                CustomerId = booking.CustomerId,
                CustomerName = booking.Customer?.CustomerName ?? "",
                ProductId = details.First().ProductId,
                ProductName = productName,
                PreviousStock = previousStock,
                TotalBooking = todayBookings,
                TotalDelivery = todayDeliveries,
                ReceiptNo = receiptNo,
                CurrentStock = currentStock,
                ReceivedRent = receivedRent
            });
        }

        return stockBookItems
            .OrderBy(x => x.CustomerName)
            .ThenBy(x => x.BookingDate)
            .ThenBy(x => x.ReceiptNo);
    }
}
