namespace Application.Services;

public class DatewiseActivityReportService : IDatewiseActivityReportService
{
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<Delivery, Guid> _deliveryRepository;
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<Customer, int> _customerRepository;
    private readonly Guid _tenantId;

    public DatewiseActivityReportService(
        IRepository<Booking, Guid> bookingRepository,
        IRepository<Delivery, Guid> deliveryRepository,
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<Customer, int> customerRepository,
        ITenantProvider tenantProvider)
    {
        _bookingRepository = bookingRepository;
        _deliveryRepository = deliveryRepository;
        _transactionRepository = transactionRepository;
        _customerRepository = customerRepository;
        _tenantId = tenantProvider.GetTenantId();
    }

    public async Task<DatewiseActivityReportResponse> GetReportAsync(
        DateTime fromDate,
        DateTime toDate,
        int? customerId,
        bool includeBookings,
        bool includeDeliveries,
        CancellationToken cancellationToken = default)
    {
        // Local calendar days -> UTC boundaries
        var fromUtc = DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Local).ToUniversalTime();
        var toUtcExclusive = DateTime.SpecifyKind(toDate.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();

        var response = new DatewiseActivityReportResponse
        {
            FromDate = fromDate.ToString("yyyy-MM-dd"),
            ToDate = toDate.ToString("yyyy-MM-dd"),
            CustomerId = customerId,
            IncludesBookings = includeBookings,
            IncludesDeliveries = includeDeliveries,
        };

        if (customerId.HasValue)
        {
            response.CustomerName = await _customerRepository.Query()
                .Where(c => c.Id == customerId.Value && c.TenantId == _tenantId)
                .Select(c => c.CustomerName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var bookingLines = includeBookings
            ? await LoadBookingLinesAsync(fromUtc, toUtcExclusive, customerId, cancellationToken)
            : [];
        var deliveryLines = includeDeliveries
            ? await LoadDeliveryLinesAsync(fromUtc, toUtcExclusive, customerId, cancellationToken)
            : [];

        var dayKeys = bookingLines.Select(b => LocalDay(b.BookingDate))
            .Concat(deliveryLines.Select(d => LocalDay(d.DeliveryDate)))
            .Distinct()
            .OrderBy(d => d);

        foreach (var day in dayKeys)
        {
            var activityDay = new ActivityDay
            {
                Date = day,
                Bookings = bookingLines.Where(b => LocalDay(b.BookingDate) == day).ToList(),
                Deliveries = deliveryLines.Where(d => LocalDay(d.DeliveryDate) == day).ToList(),
            };
            activityDay.Totals = Summarize(activityDay.Bookings, activityDay.Deliveries);
            response.Days.Add(activityDay);
        }

        response.Totals = Summarize(bookingLines, deliveryLines);
        return response;
    }

    private async Task<List<ActivityBookingLine>> LoadBookingLinesAsync(
        DateTime fromUtc, DateTime toUtcExclusive, int? customerId, CancellationToken cancellationToken)
    {
        var query = _bookingRepository.Query()
            .Where(b => b.TenantId == _tenantId && !b.IsDeleted
                        && b.BookingDate >= fromUtc && b.BookingDate < toUtcExclusive);
        if (customerId.HasValue)
            query = query.Where(b => b.CustomerId == customerId.Value);

        var lines = await query
            .SelectMany(b => b.BookingDetails.Where(bd => !bd.IsDeleted), (b, bd) => new ActivityBookingLine
            {
                BookingId = b.Id,
                BookingNumber = b.BookingNumber,
                BookingDate = b.BookingDate,
                CustomerId = b.CustomerId,
                CustomerName = b.Customer!.CustomerName,
                CustomerMobile = b.Customer!.CustomerMobile,
                ProductName = bd.Product!.ProductName,
                Quantity = bd.BookingQuantity,
                UnitName = bd.BookingUnit!.UnitName,
                Rate = bd.BookingRate,
                RentAmount = (decimal)bd.BookingQuantity * bd.BookingRate,
                LabourCharge = bd.LabourCharge,
            })
            .ToListAsync(cancellationToken);

        foreach (var line in lines)
            line.Total = line.RentAmount + line.LabourCharge;

        return lines.OrderBy(l => l.BookingDate).ThenBy(l => l.BookingNumber).ToList();
    }

    private async Task<List<ActivityDeliveryLine>> LoadDeliveryLinesAsync(
        DateTime fromUtc, DateTime toUtcExclusive, int? customerId, CancellationToken cancellationToken)
    {
        var query = _deliveryRepository.Query()
            .Where(d => d.DeliveryDate >= fromUtc && d.DeliveryDate < toUtcExclusive && !d.Booking!.IsDeleted);
        if (customerId.HasValue)
            query = query.Where(d => d.Booking!.CustomerId == customerId.Value);

        var deliveries = await query
            .Select(d => new
            {
                d.Id,
                d.DeliveryNumber,
                d.DeliveryDate,
                d.TransactionId,
                BookingNumber = d.Booking!.BookingNumber,
                CustomerId = d.Booking!.CustomerId,
                CustomerName = d.Booking!.Customer!.CustomerName,
                CustomerMobile = d.Booking!.Customer!.CustomerMobile,
                Details = d.DeliveryDetails.Where(dd => !dd.IsDeleted).Select(dd => new
                {
                    ProductName = dd.BookingDetail!.Product!.ProductName,
                    dd.DeliveryQuantity,
                    UnitName = dd.DeliveryUnit!.UnitName,
                    dd.BillingCycles,
                    dd.ChargeAmount,
                    dd.LabourCharge,
                    dd.CollectedAmount,
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        // Amount taken with each delivery comes from its payment transaction(s)
        var deliveryIds = deliveries.Select(d => (Guid?)d.Id).ToList();
        var txIds = deliveries.Where(d => d.TransactionId.HasValue).Select(d => d.TransactionId!.Value).ToList();
        var payments = await _transactionRepository.Query()
            .Where(t => t.TransactionHead != null
                        && t.TransactionHead.UsageFor == UsageFor.CUSTOMER_PAYMENT
                        && (deliveryIds.Contains(t.DeliveryId) || txIds.Contains(t.Id)))
            .Select(t => new { t.Id, t.DeliveryId, t.Amount })
            .ToListAsync(cancellationToken);

        var lines = new List<ActivityDeliveryLine>();
        foreach (var d in deliveries.OrderBy(x => x.DeliveryDate).ThenBy(x => x.DeliveryNumber))
        {
            var collected = payments
                .Where(p => p.DeliveryId == d.Id || (d.TransactionId.HasValue && p.Id == d.TransactionId.Value))
                .Sum(p => p.Amount);
            // Use the per-line split when it matches the payment; otherwise show it on the first line
            var useLineSplit = Math.Abs(d.Details.Sum(x => x.CollectedAmount) - collected) <= 0.01m;

            for (int i = 0; i < d.Details.Count; i++)
            {
                var dd = d.Details[i];
                var total = dd.ChargeAmount + dd.LabourCharge;
                var lineCollected = useLineSplit ? dd.CollectedAmount : (i == 0 ? collected : 0m);
                lines.Add(new ActivityDeliveryLine
                {
                    DeliveryId = d.Id,
                    DeliveryNumber = d.DeliveryNumber,
                    DeliveryDate = d.DeliveryDate,
                    BookingNumber = d.BookingNumber,
                    CustomerId = d.CustomerId,
                    CustomerName = d.CustomerName,
                    CustomerMobile = d.CustomerMobile,
                    ProductName = dd.ProductName,
                    Quantity = dd.DeliveryQuantity,
                    UnitName = dd.UnitName,
                    BillingCycles = dd.BillingCycles,
                    RentCharge = dd.ChargeAmount,
                    LabourCharge = dd.LabourCharge,
                    Total = total,
                    CollectedAmount = lineCollected,
                    DueAmount = total - lineCollected,
                });
            }
        }

        return lines;
    }

    private static ActivityTotals Summarize(List<ActivityBookingLine> bookings, List<ActivityDeliveryLine> deliveries) => new()
    {
        BookingCount = bookings.Select(b => b.BookingId).Distinct().Count(),
        BookingRent = bookings.Sum(b => b.RentAmount),
        BookingLabour = bookings.Sum(b => b.LabourCharge),
        BookingTotal = bookings.Sum(b => b.Total),
        DeliveryCount = deliveries.Select(d => d.DeliveryId).Distinct().Count(),
        DeliveryRent = deliveries.Sum(d => d.RentCharge),
        DeliveryLabour = deliveries.Sum(d => d.LabourCharge),
        DeliveryTotal = deliveries.Sum(d => d.Total),
        DeliveryCollected = deliveries.Sum(d => d.CollectedAmount),
        DeliveryDue = deliveries.Sum(d => d.DueAmount),
    };

    // Dates are stored as UTC; group on the local calendar day
    private static string LocalDay(DateTime utc)
        => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd");
}
