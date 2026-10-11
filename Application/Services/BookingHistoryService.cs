using Application.Contractors;

namespace Application.Services;

/// <summary>
/// Booking History report. Builds on the payment-screen data (charge ledger, payments, advances,
/// Adjustment −) so the due always matches the payment screen, and adds booking/delivery/challan
/// details, the adjustment history and a date-ordered statement with a running due.
/// </summary>
public class BookingHistoryService : IBookingHistoryService
{
    private readonly IBillCollectionService _billCollectionService;
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<Delivery, Guid> _deliveryRepository;
    private readonly IRepository<DeliveryChallanItem, Guid> _challanItemRepository;

    public BookingHistoryService(
        IBillCollectionService billCollectionService,
        IRepository<Booking, Guid> bookingRepository,
        IRepository<Delivery, Guid> deliveryRepository,
        IRepository<DeliveryChallanItem, Guid> challanItemRepository)
    {
        _billCollectionService = billCollectionService;
        _bookingRepository = bookingRepository;
        _deliveryRepository = deliveryRepository;
        _challanItemRepository = challanItemRepository;
    }

    public async Task<BookingHistoryResponse> GetHistoryAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        // Syncs the charge ledger and gives items, ledger rows, payments and the due summary
        var detail = await _billCollectionService.GetBookingPaymentDetailAsync(bookingId, cancellationToken);

        var booking = await _bookingRepository.Query()
            .Where(b => b.Id == bookingId)
            .Select(b => new { b.ReferenceNumber, BranchName = b.Branch != null ? b.Branch.Name : null })
            .FirstAsync(cancellationToken);

        var response = new BookingHistoryResponse
        {
            BookingId = detail.BookingId,
            BookingNumber = detail.BookingNumber,
            ReferenceNumber = booking.ReferenceNumber,
            BookingDate = detail.BookingDate,
            IsContracted = detail.IsContracted,
            Notes = detail.Notes,
            BranchName = booking.BranchName,
            CustomerId = detail.CustomerId,
            CustomerName = detail.CustomerName,
            CustomerMobile = detail.CustomerMobile,
            CustomerAddress = detail.CustomerAddress,
            Items = detail.Items,
            Summary = detail.Summary,
            Status = detail.Items.Any(i => i.StockInStore > 0.0001m) ? "RUNNING" : "COMPLETED",
            Deliveries = await LoadDeliveriesAsync(bookingId, detail, cancellationToken),
            Adjustments = await _billCollectionService.GetAdjustmentHistoryAsync(detail.CustomerId, bookingId, cancellationToken),
            GeneratedAt = DateTime.UtcNow,
        };
        response.Statement = BuildStatement(detail);
        return response;
    }

    private async Task<List<BookingHistoryDelivery>> LoadDeliveriesAsync(
        Guid bookingId, BookingPaymentDetailResponse detail, CancellationToken cancellationToken)
    {
        var deliveries = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails).ThenInclude(dd => dd.BookingDetail).ThenInclude(bd => bd!.Product)
            .Include(d => d.DeliveryDetails).ThenInclude(dd => dd.DeliveryUnit)
            .Where(d => d.BookingId == bookingId && !d.IsArchived)
            .OrderBy(d => d.DeliveryDate)
            .ToListAsync(cancellationToken);
        var deliveryIds = deliveries.Select(d => d.Id).ToList();

        var challans = await _challanItemRepository.Query()
            .Where(ci => deliveryIds.Contains(ci.DeliveryId) && ci.DeliveryChallan != null && !ci.DeliveryChallan.IsDeleted)
            .Select(ci => new
            {
                ci.DeliveryId,
                ci.DeliveryChallan!.ChallanNumber,
                ci.DeliveryChallan.VehicleNumber,
                ci.DeliveryChallan.DriverName,
            })
            .ToListAsync(cancellationToken);
        var challanByDelivery = challans.GroupBy(c => c.DeliveryId).ToDictionary(g => g.Key, g => g.First());

        // Money taken with each delivery, as shown on the payment screen (first line of each delivery)
        var collectedByNumber = detail.Deliveries
            .GroupBy(d => d.DeliveryNumber)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.CollectedAmount));

        return deliveries.Select(d =>
        {
            var challan = challanByDelivery.GetValueOrDefault(d.Id);
            return new BookingHistoryDelivery
            {
                DeliveryId = d.Id,
                DeliveryNumber = d.DeliveryNumber,
                DeliveryDate = d.DeliveryDate,
                Notes = d.Notes,
                PaymentStatus = d.PaymentStatus,
                ChallanNumber = challan?.ChallanNumber,
                VehicleNumber = challan?.VehicleNumber,
                DriverName = challan?.DriverName,
                AdjustmentValue = d.AdjustmentValue,
                CollectedAmount = collectedByNumber.GetValueOrDefault(d.DeliveryNumber),
                Lines = d.DeliveryDetails.Where(dd => !dd.IsDeleted).Select(dd => new BookingHistoryDeliveryLine
                {
                    ProductName = dd.BookingDetail?.Product?.ProductName ?? string.Empty,
                    Quantity = dd.DeliveryQuantity,
                    UnitName = dd.DeliveryUnit?.UnitName ?? string.Empty,
                    BillingCycles = dd.BillingCycles,
                    RentCharge = dd.ChargeAmount,
                    LabourCharge = dd.LabourCharge,
                }).ToList(),
            };
        }).ToList();
    }

    /// <summary>Charges and money in, in date order (charges first on the same day), with a running due.</summary>
    private static List<BookingStatementLine> BuildStatement(BookingPaymentDetailResponse detail)
    {
        var lines = new List<(BookingStatementLine Line, int Order, DateTime Created)>();

        foreach (var e in detail.LedgerEntries)
        {
            var (category, particulars) = DescribeCharge(e);
            lines.Add((new BookingStatementLine
            {
                Date = e.EntryDate,
                Category = category,
                Particulars = particulars,
                Reference = e.ReferenceCode ?? e.DeliveryNumber,
                Charge = e.Amount,
            }, 0, e.CreatedAt));
        }

        foreach (var p in detail.Payments)
        {
            var atDelivery = p.Kind == "AT_DELIVERY";
            lines.Add((new BookingStatementLine
            {
                Date = p.Date,
                Category = atDelivery ? "AT_DELIVERY" : "PAYMENT",
                Particulars = $"{(atDelivery ? "ডেলিভারিতে পরিশোধ / Paid at delivery" : "পরিশোধ / Payment")} ({MethodLabel(p.PaymentMethod)})",
                Reference = p.ReferenceCode,
                Paid = p.Amount,
            }, 1, p.Date));
        }

        foreach (var a in detail.AdvanceApplications)
        {
            lines.Add((new BookingStatementLine
            {
                Date = a.Date,
                Category = "ADVANCE",
                Particulars = $"অগ্রিম থেকে / Advance applied — {a.Description}",
                Reference = a.ReferenceCode,
                Paid = a.Amount,
            }, 1, a.Date));
        }

        foreach (var d in detail.Discounts)
        {
            lines.Add((new BookingStatementLine
            {
                Date = d.Date,
                Category = "ADJUSTMENT_MINUS",
                Particulars = $"সমন্বয় (−) / Adjustment − : {d.Description}",
                Reference = d.ReferenceCode,
                Paid = d.Amount,
            }, 1, d.Date));
        }

        var balance = 0m;
        var ordered = lines
            .OrderBy(l => ReportDateWindow.ToLocalDay(l.Line.Date))
            .ThenBy(l => l.Order)
            .ThenBy(l => l.Created)
            .Select(l => l.Line)
            .ToList();
        foreach (var line in ordered)
        {
            balance += line.Charge - line.Paid;
            line.Balance = Math.Round(balance, 2);
        }
        return ordered;
    }

    private static (string Category, string Particulars) DescribeCharge(LedgerEntryResponse e)
    {
        var product = string.IsNullOrEmpty(e.ProductName) ? string.Empty : $" — {e.ProductName}";
        var description = e.ChargeType switch
        {
            ChargeTypes.RentCycle =>
                $"ভাড়া / Rent – month {e.CycleNo}{product} ({e.Quantity:0.##} × {e.Rate:0.##}, {ReportDateWindow.ToLocalDay(e.BillPeriodFrom):dd-MMM-yy} → {ReportDateWindow.ToLocalDay(e.BillPeriodTo):dd-MMM-yy})",
            ChargeTypes.BookingLabour => $"বুকিং লেবার / Booking labour{product}",
            ChargeTypes.DeliveryLabour => $"ডেলিভারি লেবার / Delivery labour{product}",
            ChargeTypes.DeliveryAdjustment => "ডেলিভারি সমন্বয় / Delivery adjustment",
            ChargeTypes.ManualAdjustment => e.Source == RecurringChargeSources.Reversal
                ? $"সমন্বয় (+) বাতিল / Adjustment + cancelled : {e.Note}"
                : $"সমন্বয় (+) / Adjustment + : {e.Note}",
            _ => e.ChargeType,
        };

        if (e.Source == RecurringChargeSources.Correction)
            return ("CORRECTION", $"সংশোধন / Correction – {description}. {e.Note}");

        var category = e.ChargeType switch
        {
            ChargeTypes.RentCycle => "RENT",
            ChargeTypes.BookingLabour or ChargeTypes.DeliveryLabour => "LABOUR",
            ChargeTypes.DeliveryAdjustment => "DELIVERY_ADJUSTMENT",
            ChargeTypes.ManualAdjustment => "ADJUSTMENT_PLUS",
            _ => e.ChargeType,
        };
        return (category, description);
    }

    private static string MethodLabel(string? method) => method switch
    {
        PaymentMethods.CASH => "Cash",
        PaymentMethods.BANK_TRANSFER => "Bank transfer",
        PaymentMethods.CHEQUE => "Cheque",
        PaymentMethods.MOBILE_BANKING => "Mobile banking",
        PaymentMethods.CARD => "Card",
        null or "" => "-",
        _ => method,
    };
}
