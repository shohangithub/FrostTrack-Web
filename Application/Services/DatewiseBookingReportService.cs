using Application.Contractors;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class DatewiseBookingReportService : IDatewiseBookingReportService
{
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly Guid _tenantId;

    public DatewiseBookingReportService(
        IRepository<Booking, Guid> bookingRepository,
        ITenantProvider tenantProvider)
    {
        _bookingRepository = bookingRepository;
        _tenantId = tenantProvider.GetTenantId();
    }

    public async Task<IEnumerable<DatewiseBookingReportResponse>> GetDatewiseBookingReportAsync(
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

        var bookings = await query
            .OrderBy(b => b.BookingDate)
            .ThenBy(b => b.Customer!.CustomerName)
            .ToListAsync(cancellationToken);

        // Build report items
        var reportItems = new List<DatewiseBookingReportResponse>();

        foreach (var booking in bookings)
        {
            foreach (var detail in booking.BookingDetails)
            {
                // Skip if product filter specified and doesn't match
                if (productId.HasValue && detail.ProductId != productId.Value)
                    continue;

                reportItems.Add(new DatewiseBookingReportResponse
                {
                    BookingId = booking.Id,
                    BookingCode = booking.BookingNumber ?? "",
                    BookingDate = booking.BookingDate,
                    CustomerId = booking.CustomerId,
                    CustomerName = booking.Customer?.CustomerName ?? "",
                    CustomerMobile = booking.Customer?.CustomerMobile ?? "",
                    ProductId = detail.ProductId,
                    ProductName = detail.Product?.ProductName ?? "",
                    BookingQuantity = detail.BookingQuantity,
                    RentRate = detail.BookingRate,
                    TotalAmount = (decimal)detail.BookingQuantity * detail.BookingRate,
                    Remarks = booking.Notes ?? ""
                });
            }
        }

        return reportItems;
    }
}
