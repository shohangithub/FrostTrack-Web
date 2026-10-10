namespace Application.Services;

/// <summary>
/// Builds a period bill for a customer: bookings and deliveries (with labour) inside the period,
/// payments and discounts received, and the due carried in / out.
/// Dues use the same accrual rules as the customer due report (BookingDueCalculator).
/// </summary>
public class CustomerBillStatementService : ICustomerBillStatementService
{
    private readonly IRepository<Customer, int> _customerRepository;
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<Delivery, Guid> _deliveryRepository;
    private readonly IRepository<Transaction, Guid> _transactionRepository;

    public CustomerBillStatementService(
        IRepository<Customer, int> customerRepository,
        IRepository<Booking, Guid> bookingRepository,
        IRepository<Delivery, Guid> deliveryRepository,
        IRepository<Transaction, Guid> transactionRepository)
    {
        _customerRepository = customerRepository;
        _bookingRepository = bookingRepository;
        _deliveryRepository = deliveryRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<List<Lookup<int>>> GetContractCustomersAsync(CancellationToken cancellationToken = default)
    {
        return await _bookingRepository.Query()
            .Where(b => b.IsContracted && !b.IsDeleted && !b.IsArchived && !b.Customer!.IsDeleted)
            .Select(b => new { b.CustomerId, b.Customer!.CustomerName, b.Customer.CustomerMobile })
            .Distinct()
            .OrderBy(c => c.CustomerName)
            .Select(c => new Lookup<int>(c.CustomerId,
                c.CustomerMobile != null && c.CustomerMobile != "" ? c.CustomerName + " (" + c.CustomerMobile + ")" : c.CustomerName))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerBillStatementResponse> GetStatementAsync(
        int customerId,
        DateTime fromUtc,
        DateTime toUtcExclusive,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.Query()
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw new Exception("Customer not found");

        var bookings = await _bookingRepository.Query()
            .Include(b => b.BookingDetails).ThenInclude(bd => bd.Product)
            .Include(b => b.BookingDetails).ThenInclude(bd => bd.BookingUnit)
            .Where(b => b.CustomerId == customerId && b.IsContracted && !b.IsDeleted && !b.IsArchived && b.BookingDate < toUtcExclusive)
            .OrderBy(b => b.BookingDate)
            .ToListAsync(cancellationToken);

        var bookingIds = bookings.Select(b => b.Id).ToList();

        // Only contract (চুক্তি) bookings are billed here. Payments/discounts not linked to any booking are
        // counted only when the customer has no regular bookings; otherwise they belong to the regular due.
        var hasRegularBookings = await _bookingRepository.Query()
            .AnyAsync(b => b.CustomerId == customerId && !b.IsContracted && !b.IsDeleted && !b.IsArchived, cancellationToken);
        var includeUnlinked = !hasRegularBookings;

        var deliveries = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails).ThenInclude(dd => dd.BookingDetail).ThenInclude(bd => bd!.Product)
            .Include(d => d.DeliveryDetails).ThenInclude(dd => dd.DeliveryUnit)
            .Where(d => bookingIds.Contains(d.BookingId) && d.DeliveryDate < toUtcExclusive)
            .OrderBy(d => d.DeliveryDate)
            .ToListAsync(cancellationToken);

        var payments = await _transactionRepository.Query()
            .Where(t => ((t.BookingId.HasValue && bookingIds.Contains(t.BookingId.Value))
                         || (includeUnlinked && !t.BookingId.HasValue && t.CustomerId == customerId))
                        && t.TransactionDate < toUtcExclusive
                        && t.TransactionHead != null
                        && t.TransactionHead.Type == TransactionHeadTypes.DEBIT
                        && t.TransactionHead.UsageFor == UsageFor.CUSTOMER_PAYMENT)
            .OrderBy(t => t.TransactionDate)
            .Select(t => new BillStatementPaymentLine
            {
                TransactionId = t.Id,
                DeliveryId = t.DeliveryId,
                Date = t.TransactionDate,
                TransactionCode = t.TransactionCode,
                PaymentMethod = t.PaymentMethod,
                Description = t.Description,
                Amount = t.Amount
            })
            .ToListAsync(cancellationToken);

        var discounts = await _transactionRepository.Query()
            .Where(t => ((t.BookingId.HasValue && bookingIds.Contains(t.BookingId.Value))
                         || (includeUnlinked && !t.BookingId.HasValue && t.CustomerId == customerId))
                        && t.TransactionDate < toUtcExclusive
                        && t.TransactionHead != null
                        && t.TransactionHead.UsageFor == UsageFor.BILL_DISCOUNT)
            .OrderBy(t => t.TransactionDate)
            .Select(t => new BillStatementPaymentLine
            {
                Date = t.TransactionDate,
                TransactionCode = t.TransactionCode,
                PaymentMethod = t.PaymentMethod,
                Description = t.DiscountReason ?? t.Description,
                Amount = t.Amount > 0 ? t.Amount : t.DiscountAmount
            })
            .ToListAsync(cancellationToken);

        var response = new CustomerBillStatementResponse
        {
            CustomerId = customer.Id,
            CustomerName = customer.CustomerName,
            CustomerMobile = customer.CustomerMobile,
            Address = customer.Address,
            FromDate = fromUtc,
            ToDate = toUtcExclusive.AddTicks(-1),
        };

        // ---- Bookings received in the period ----
        foreach (var booking in bookings.Where(b => b.BookingDate >= fromUtc))
        {
            foreach (var bd in booking.BookingDetails.Where(d => !d.IsDeleted))
            {
                var rent = (decimal)bd.BookingQuantity * bd.BookingRate;
                response.Bookings.Add(new BillStatementBookingLine
                {
                    BookingId = booking.Id,
                    BookingNumber = booking.BookingNumber,
                    BookingDate = booking.BookingDate,
                    ProductName = bd.Product?.ProductName ?? string.Empty,
                    UnitName = bd.BookingUnit?.UnitName ?? string.Empty,
                    Quantity = bd.BookingQuantity,
                    BillType = bd.BillType,
                    Rate = bd.BookingRate,
                    RentAmount = rent,
                    LabourCharge = bd.LabourCharge,
                    Total = rent + bd.LabourCharge
                });
            }
        }

        // ---- Link payments to the delivery they were collected with ----
        var deliveryByTransaction = deliveries
            .Where(d => d.TransactionId.HasValue)
            .GroupBy(d => d.TransactionId!.Value)
            .ToDictionary(g => g.Key, g => g.First());
        var deliveryById = deliveries.ToDictionary(d => d.Id);
        foreach (var p in payments)
        {
            var linked = p.DeliveryId.HasValue && deliveryById.TryGetValue(p.DeliveryId.Value, out var byId)
                ? byId
                : deliveryByTransaction.GetValueOrDefault(p.TransactionId);
            if (linked == null) continue;
            p.DeliveryId = linked.Id;
            p.DeliveryNumber = linked.DeliveryNumber;
        }

        // ---- Deliveries made in the period ----
        var bookingNumbers = bookings.ToDictionary(b => b.Id, b => b.BookingNumber);
        foreach (var delivery in deliveries.Where(d => d.DeliveryDate >= fromUtc))
        {
            var lines = delivery.DeliveryDetails.Where(x => !x.IsDeleted).ToList();

            // Amount collected with this delivery comes from its payment transaction(s);
            // use the per-line split when it matches, otherwise show it on the first line.
            var collected = payments.Where(p => p.DeliveryId == delivery.Id).Sum(p => p.Amount);
            var useLineSplit = Math.Abs(lines.Sum(x => x.CollectedAmount) - collected) <= 0.01m;

            for (int i = 0; i < lines.Count; i++)
            {
                var dd = lines[i];
                var lineCollected = useLineSplit ? dd.CollectedAmount : (i == 0 ? collected : 0m);
                var lineTotal = dd.ChargeAmount + dd.LabourCharge;
                response.Deliveries.Add(new BillStatementDeliveryLine
                {
                    DeliveryId = delivery.Id,
                    DeliveryNumber = delivery.DeliveryNumber,
                    DeliveryDate = delivery.DeliveryDate,
                    BookingNumber = bookingNumbers.GetValueOrDefault(delivery.BookingId, string.Empty),
                    ProductName = dd.BookingDetail?.Product?.ProductName ?? string.Empty,
                    UnitName = dd.DeliveryUnit?.UnitName ?? string.Empty,
                    Quantity = dd.DeliveryQuantity,
                    BillingCycles = dd.BillingCycles,
                    Rate = dd.BookingDetail?.BookingRate ?? 0m,
                    RentCharge = dd.ChargeAmount,
                    LabourCharge = dd.LabourCharge,
                    Total = lineTotal,
                    CollectedAmount = lineCollected,
                    DueAmount = lineTotal - lineCollected
                });
            }
            response.DeliveryAdjustmentTotal += delivery.AdjustmentValue;
        }

        response.Payments = payments.Where(p => p.Date >= fromUtc).ToList();
        response.Discounts = discounts.Where(p => p.Date >= fromUtc).ToList();

        response.BookingRentTotal = response.Bookings.Sum(x => x.RentAmount);
        response.BookingLabourTotal = response.Bookings.Sum(x => x.LabourCharge);
        response.DeliveryRentTotal = response.Deliveries.Sum(x => x.RentCharge);
        response.DeliveryLabourTotal = response.Deliveries.Sum(x => x.LabourCharge);
        response.DeliveryCollectedTotal = response.Deliveries.Sum(x => x.CollectedAmount);
        response.PaidAtDelivery = response.Payments.Where(p => p.DeliveryId.HasValue).Sum(p => p.Amount);
        response.OtherPayments = response.Payments.Where(p => !p.DeliveryId.HasValue).Sum(p => p.Amount);

        // ---- Stock in store and storage rent per booking line ----
        // Same rule as the customer due report: delivered goods carry their delivery rent,
        // goods still in store carry stock x rate x (first cycle + completed cycles).
        decimal rentBefore = 0m, rentAtEnd = 0m, labourBefore = 0m, labourAtEnd = 0m;

        foreach (var booking in bookings)
        {
            foreach (var bd in booking.BookingDetails.Where(d => !d.IsDeleted))
            {
                var lineDeliveries = deliveries
                    .SelectMany(d => d.DeliveryDetails.Where(dd => !dd.IsDeleted && dd.BookingDetailId == bd.Id),
                        (d, dd) => (d.DeliveryDate, Detail: dd))
                    .ToList();

                var start = LineState(booking, bd, lineDeliveries, fromUtc);
                var end = LineState(booking, bd, lineDeliveries, toUtcExclusive);

                rentBefore += start.Rent;
                rentAtEnd += end.Rent;
                labourBefore += start.Labour;
                labourAtEnd += end.Labour;

                var bookedInPeriod = booking.BookingDate >= fromUtc;
                var deliveredInPeriod = end.DeliveredQty - start.DeliveredQty;
                var rentThisPeriod = end.Rent - start.Rent;

                // Lines that were fully delivered before the period and did not change are left out
                if (!bookedInPeriod && start.Stock <= 0 && end.Stock <= 0 && rentThisPeriod == 0m)
                    continue;

                var billingDates = BillingDatesInPeriod(booking.BookingDate, bd.BillType, fromUtc, toUtcExclusive);
                if (end.Stock <= 0m && lineDeliveries.Count > 0)
                {
                    // Fully delivered: cycles after the last delivery are not charged
                    var lastDelivery = lineDeliveries.Where(x => x.DeliveryDate < toUtcExclusive).Max(x => x.DeliveryDate);
                    billingDates = billingDates.Where(d => d <= lastDelivery).ToList();
                }

                response.StockLines.Add(new BillStatementStockLine
                {
                    BookingId = booking.Id,
                    BookingNumber = booking.BookingNumber,
                    BookingDate = booking.BookingDate,
                    ProductName = bd.Product?.ProductName ?? string.Empty,
                    UnitName = bd.BookingUnit?.UnitName ?? string.Empty,
                    BillType = bd.BillType,
                    Rate = bd.BookingRate,
                    OpeningStock = bookedInPeriod ? 0m : start.Stock,
                    BookedQty = bookedInPeriod ? (decimal)bd.BookingQuantity : 0m,
                    DeliveredQty = deliveredInPeriod,
                    ClosingStock = end.Stock,
                    BillingDates = billingDates.Take(12).ToList(),
                    CyclesInPeriod = billingDates.Count,
                    RentThisPeriod = rentThisPeriod
                });
            }
        }

        // Delivery-level adjustments are part of the accrued rent as well
        var adjustmentBefore = deliveries.Where(d => d.DeliveryDate < fromUtc).Sum(d => d.AdjustmentValue);

        // ---- Account summary ----
        var paidBefore = payments.Where(p => p.Date < fromUtc).Sum(p => p.Amount);
        var discountBefore = discounts.Where(p => p.Date < fromUtc).Sum(p => p.Amount);

        var openingBalance = includeUnlinked ? customer.OpeningBalance : 0m;
        response.PreviousDue = openingBalance + rentBefore + adjustmentBefore + labourBefore - paidBefore - discountBefore;
        response.RentCharged = rentAtEnd - rentBefore + response.DeliveryAdjustmentTotal;
        response.LabourCharged = labourAtEnd - labourBefore;
        response.TotalCharged = response.RentCharged + response.LabourCharged;
        response.TotalPaid = response.Payments.Sum(p => p.Amount);
        response.TotalDiscount = response.Discounts.Sum(p => p.Amount);
        response.ClosingDue = response.PreviousDue + response.TotalCharged - response.TotalPaid - response.TotalDiscount;

        return response;
    }

    private readonly record struct LineAccrual(decimal Stock, decimal DeliveredQty, decimal Rent, decimal Labour);

    /// <summary>
    /// Stock, delivered quantity, accrued rent and labour of one booking line counting everything dated before
    /// <paramref name="cutoffUtc"/>. Mirrors BookingDueCalculator for a single line.
    /// </summary>
    private static LineAccrual LineState(
        Booking booking,
        BookingDetail bd,
        List<(DateTime DeliveryDate, DeliveryDetail Detail)> lineDeliveries,
        DateTime cutoffUtc)
    {
        if (booking.BookingDate >= cutoffUtc)
            return new LineAccrual(0m, 0m, 0m, 0m);

        var delivered = lineDeliveries.Where(x => x.DeliveryDate < cutoffUtc).Select(x => x.Detail).ToList();
        var deliveredBase = delivered.Sum(dd => dd.BaseQuantity);
        var deliveredRent = delivered.Sum(dd => dd.ChargeAmount);
        var deliveredLabour = delivered.Sum(dd => dd.LabourCharge);

        // Booking unit <-> base unit factor
        var baseFactor = bd.BookingQuantity > 0 ? bd.BaseQuantity / (decimal)bd.BookingQuantity : 1m;
        var deliveredQty = baseFactor > 0 ? deliveredBase / baseFactor : 0m;

        var remainingRatio = bd.BaseQuantity > 0
            ? Math.Max(0m, (bd.BaseQuantity - deliveredBase) / bd.BaseQuantity)
            : 0m;
        var stock = (decimal)bd.BookingQuantity * remainingRatio;

        var cycles = 1 + RecurringChargeCalculator.CompletedCycles(bd.BillType, booking.BookingDate, cutoffUtc.AddTicks(-1));
        var rent = deliveredRent + stock * bd.BookingRate * cycles;

        return new LineAccrual(stock, deliveredQty, rent, bd.LabourCharge + deliveredLabour);
    }

    /// <summary>
    /// Start dates of the billing cycles that fall inside the period. The booking date starts cycle 1;
    /// later cycles start the day after each anniversary (see RecurringChargeCalculator).
    /// </summary>
    private static List<DateTime> BillingDatesInPeriod(DateTime bookingDate, string billType, DateTime fromUtc, DateTime toUtcExclusive)
    {
        var dates = new List<DateTime>();
        var firstK = Math.Max(0, RecurringChargeCalculator.CompletedCycles(billType, bookingDate, fromUtc) - 1);
        for (int k = firstK; k < firstK + 2000; k++)
        {
            var cycleStart = RecurringChargeCalculator.CycleStart(billType, bookingDate, k);
            if (cycleStart >= toUtcExclusive) break;
            if (cycleStart >= fromUtc) dates.Add(cycleStart);
        }
        return dates;
    }
}
