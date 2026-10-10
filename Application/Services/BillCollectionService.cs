using Application.Contractors;
using Application.Contractors.Authentication;
using Application.Framework;
using Application.ReponseDTO;
using Application.RequestDTO;
using Application.Services.Common;
using Domain.Entitites;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class BillCollectionService : IBillCollectionService
{
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<TransactionHead, Guid> _transactionHeadRepository;
    private readonly IRepository<Delivery, Guid> _deliveryRepository;
    private readonly IRepository<BankTransaction, long> _bankTransactionRepository;
    private readonly IRepository<Bank, int> _bankRepository;
    private readonly IRepository<Customer, int> _customerRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly IRepository<AdvanceAllocation, Guid> _advanceAllocationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEditWindowGuard _editWindowGuard;
    private readonly IBookingLedgerService _ledgerService;
    private readonly Guid _tenantId;

    public BillCollectionService(
        IRepository<Booking, Guid> bookingRepository,
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<TransactionHead, Guid> transactionHeadRepository,
        IRepository<Delivery, Guid> deliveryRepository,
        IRepository<BankTransaction, long> bankTransactionRepository,
        IRepository<Bank, int> bankRepository,
        IRepository<Customer, int> customerRepository,
        DefaultValueInjector defaultValueInjector,
        ITenantProvider tenantProvider,
        IRepository<AdvanceAllocation, Guid> advanceAllocationRepository,
        IUnitOfWork unitOfWork,
        IEditWindowGuard editWindowGuard,
        IBookingLedgerService ledgerService)
    {
        _ledgerService = ledgerService;
        _advanceAllocationRepository = advanceAllocationRepository;
        _unitOfWork = unitOfWork;
        _editWindowGuard = editWindowGuard;
        _bookingRepository = bookingRepository;
        _transactionRepository = transactionRepository;
        _transactionHeadRepository = transactionHeadRepository;
        _deliveryRepository = deliveryRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _bankRepository = bankRepository;
        _customerRepository = customerRepository;
        _defaultValueInjector = defaultValueInjector;
        _tenantId = tenantProvider.GetTenantId();
    }

    public async Task<CustomerBalanceSummaryResponse> GetCustomerBalanceSummaryAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
            throw new Exception("Customer not found");

        var bookings = await _bookingRepository.Query()
            .Include(b => b.BookingDetails)
            .Where(b => b.CustomerId == customerId && !b.IsDeleted && !b.IsArchived)
            .ToListAsync(cancellationToken);

        var bookingIds = bookings.Select(b => b.Id).ToList();

        var deliveries = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails)
            .Where(d => bookingIds.Contains(d.BookingId) && !d.IsDeleted && !d.IsArchived)
            .ToListAsync(cancellationToken);

        var deliveriesByBooking = deliveries
            .GroupBy(d => d.BookingId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var payments = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Include(t => t.Bank)
            .Where(t => !t.IsDeleted
                        && !t.IsArchived
                        && ((t.BookingId.HasValue && bookingIds.Contains(t.BookingId.Value)) || (t.CustomerId == customerId))
                        && t.TransactionHead != null
                        && t.TransactionHead.Type == TransactionHeadTypes.DEBIT
                        && t.TransactionHead.UsageFor == UsageFor.CUSTOMER_PAYMENT)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

        // Charges from the charge ledger (synced first so it is current)
        await _ledgerService.SyncBookingsAsync(bookingIds, cancellationToken: cancellationToken);
        var ledger = await _ledgerService.GetChargesAsync(bookingIds, DateTime.UtcNow, cancellationToken);
        decimal totalBookingCharges = ledger.Values.Sum(c => c.RentFirstMonth + c.BookingLabour);
        decimal totalDeliveryCharges = ledger.Values.Sum(c => c.DeliveryLabour + c.Adjustment);
        decimal totalRecurringCharges = ledger.Values.Sum(c => c.RentLaterMonths);

        var discountTrxs = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t => !t.IsDeleted
                        && !t.IsArchived
                        && ((t.BookingId.HasValue && bookingIds.Contains(t.BookingId.Value)) || (t.CustomerId == customerId))
                        && t.TransactionHead != null
                        && t.TransactionHead.UsageFor == UsageFor.BILL_DISCOUNT)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

        var openingBalance = customer.OpeningBalance;
        var totalAccrued = openingBalance + totalBookingCharges + totalDeliveryCharges + totalRecurringCharges;
        var totalPaid = payments.Sum(p => p.Amount);
        var totalDiscounts = discountTrxs.Sum(d => d.Amount > 0 ? d.Amount : d.DiscountAmount);
        var netDue = Math.Max(0m, totalAccrued - totalPaid - totalDiscounts);

        var recentPayments = payments.Take(10).Select(p => new RecentCustomerPaymentDto(
            p.Id,
            p.TransactionCode,
            p.TransactionDate,
            p.Amount,
            p.PaymentMethod,
            p.PaymentReference,
            p.Note,
            p.Bank?.BankName
        )).ToList();

        return new CustomerBalanceSummaryResponse(
            customerId,
            customer.CustomerName,
            customer.CustomerMobile ?? string.Empty,
            openingBalance,
            totalBookingCharges,
            totalDeliveryCharges,
            totalRecurringCharges,
            totalAccrued,
            totalPaid,
            totalDiscounts,
            netDue,
            bookings.Count,
            recentPayments
        );
    }

    public async Task<TransactionResponse> CreateCustomerPaymentAsync(
        CustomerPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CustomerId <= 0)
            throw new Exception("Please select a valid customer");

        if (request.Amount <= 0)
            throw new Exception("Payment amount must be greater than zero");

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null || customer.IsDeleted)
            throw new Exception("Customer not found");

        string description;
        var refPart = string.IsNullOrWhiteSpace(request.PaymentReference) ? "" : $" (Ref: {request.PaymentReference})";
        if (request.BookingId.HasValue)
        {
            // A booking payment can never be more than that booking's due
            var ledger = await LoadBookingLedgerAsync(request.BookingId.Value, cancellationToken);
            if (ledger.Booking.CustomerId != request.CustomerId)
                throw new Exception("The selected booking does not belong to this customer");
            var due = Math.Max(ledger.Summary.Due, 0m);
            if (request.Amount > due + 0.01m)
                throw new Exception($"Payment (৳{request.Amount:N2}) cannot be more than the booking due (৳{due:N2})");
            description = $"Customer Payment - {customer.CustomerName} - Booking {ledger.Booking.BookingNumber}{refPart}";
        }
        else
        {
            // No booking: the money is kept as customer advance
            description = $"Customer Advance - {customer.CustomerName}{refPart}";
        }

        var transactionHead = await GetOrCreateCustomerPaymentHeadAsync(cancellationToken);
        var entity = await AddCustomerPaymentTransactionAsync(
            transactionHead, customer, request.TransactionCode, request.TransactionDate, request.BranchId,
            request.Amount, request.PaymentMethod, request.PaymentReference, request.Note, request.BankId,
            request.BookingId, description, cancellationToken);

        return entity.Adapt<TransactionResponse>();
    }

    #region Booking-wise payment

    public async Task<CustomerDueBookingsResponse> GetCustomerDueBookingsAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null || customer.IsDeleted)
            throw new Exception("Customer not found");

        var bookings = await _bookingRepository.Query()
            .Include(b => b.BookingDetails)
            .Where(b => b.CustomerId == customerId && !b.IsDeleted && !b.IsArchived)
            .OrderBy(b => b.BookingDate)
            .ToListAsync(cancellationToken);
        var bookingIds = bookings.Select(b => b.Id).ToList();

        var deliveries = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails)
            .Where(d => bookingIds.Contains(d.BookingId) && !d.IsArchived)
            .ToListAsync(cancellationToken);
        var paid = await SumByBookingAsync(bookingIds, UsageFor.CUSTOMER_PAYMENT, cancellationToken);
        var discounted = await SumByBookingAsync(bookingIds, UsageFor.BILL_DISCOUNT, cancellationToken);
        var allocated = await SumAllocationsByBookingAsync(bookingIds, cancellationToken);
        await _ledgerService.SyncBookingsAsync(bookingIds, cancellationToken: cancellationToken);
        var ledger = await _ledgerService.GetChargesAsync(bookingIds, DateTime.UtcNow, cancellationToken);

        var now = DateTime.UtcNow;
        var response = new CustomerDueBookingsResponse
        {
            CustomerId = customer.Id,
            CustomerName = customer.CustomerName,
            CustomerMobile = customer.CustomerMobile,
            AdvanceBalance = await GetAdvanceBalanceAsync(customerId, cancellationToken),
        };

        foreach (var booking in bookings)
        {
            var accrued = ledger.GetValueOrDefault(booking.Id)?.Total ?? 0m;
            var due = accrued
                - paid.GetValueOrDefault(booking.Id)
                - discounted.GetValueOrDefault(booking.Id)
                - allocated.GetValueOrDefault(booking.Id);

            if (due > 0.005m)
            {
                response.Bookings.Add(new DueBookingOption
                {
                    BookingId = booking.Id,
                    BookingNumber = booking.BookingNumber,
                    BookingDate = booking.BookingDate,
                    IsContracted = booking.IsContracted,
                    Due = Math.Round(due, 2),
                });
            }
        }

        return response;
    }

    public async Task<BookingPaymentDetailResponse> GetBookingPaymentDetailAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var ledger = await LoadBookingLedgerAsync(bookingId, cancellationToken);
        var booking = ledger.Booking;

        var response = new BookingPaymentDetailResponse
        {
            BookingId = booking.Id,
            BookingNumber = booking.BookingNumber,
            BookingDate = booking.BookingDate,
            IsContracted = booking.IsContracted,
            Notes = booking.Notes,
            CustomerId = booking.CustomerId,
            CustomerName = booking.Customer?.CustomerName ?? string.Empty,
            CustomerMobile = booking.Customer?.CustomerMobile,
            CustomerAddress = booking.Customer?.Address,
            Summary = ledger.Summary,
            AvailableAdvance = await GetAdvanceBalanceAsync(booking.CustomerId, cancellationToken),
            Payments = ledger.Payments,
            AdvanceApplications = ledger.AdvanceApplications,
            Discounts = ledger.Discounts,
        };

        var ledgerEntries = await _ledgerService.GetEntriesAsync(bookingId, cancellationToken);
        response.LedgerEntries = ledgerEntries;
        var asOf = DateTime.UtcNow;
        var currentMonth = 1;
        foreach (var bd in booking.BookingDetails.Where(d => !d.IsDeleted))
        {
            var lineDeliveries = ledger.Deliveries
                .SelectMany(d => d.DeliveryDetails.Where(dd => !dd.IsDeleted && dd.BookingDetailId == bd.Id))
                .ToList();
            var deliveredBase = lineDeliveries.Sum(dd => dd.BaseQuantity);
            var baseFactor = bd.BookingQuantity > 0 ? bd.BaseQuantity / (decimal)bd.BookingQuantity : 1m;
            var stock = bd.BaseQuantity > 0
                ? (decimal)bd.BookingQuantity * Math.Max(0m, (bd.BaseQuantity - deliveredBase) / bd.BaseQuantity)
                : 0m;
            currentMonth = 1 + RecurringChargeCalculator.CompletedCycles(bd.BillType, booking.BookingDate, asOf);

            response.Items.Add(new BookingPaymentItemLine
            {
                ProductName = bd.Product?.ProductName ?? string.Empty,
                UnitName = bd.BookingUnit?.UnitName ?? string.Empty,
                BillType = bd.BillType,
                Rate = bd.BookingRate,
                BookedQty = (decimal)bd.BookingQuantity,
                DeliveredQty = baseFactor > 0 ? deliveredBase / baseFactor : 0m,
                StockInStore = stock,
                CurrentMonth = currentMonth,
                // Rent actually charged for this line (charge ledger) and months charged so far
                StockRent = ledgerEntries.Where(e => e.ChargeType == ChargeTypes.RentCycle && e.BookingDetailId == bd.Id).Sum(e => e.Amount),
                DeliveredRent = lineDeliveries.Sum(dd => dd.ChargeAmount),
                BookingLabour = bd.LabourCharge,
                DeliveryLabour = lineDeliveries.Sum(dd => dd.LabourCharge),
            });
        }

        foreach (var delivery in ledger.Deliveries.OrderBy(d => d.DeliveryDate))
        {
            var lines = delivery.DeliveryDetails.Where(dd => !dd.IsDeleted).ToList();
            var collected = ledger.CollectedByDelivery.GetValueOrDefault(delivery.Id);
            for (int i = 0; i < lines.Count; i++)
            {
                var dd = lines[i];
                response.Deliveries.Add(new BookingPaymentDeliveryLine
                {
                    DeliveryNumber = delivery.DeliveryNumber,
                    DeliveryDate = delivery.DeliveryDate,
                    ProductName = dd.BookingDetail?.Product?.ProductName ?? string.Empty,
                    Quantity = dd.DeliveryQuantity,
                    UnitName = dd.DeliveryUnit?.UnitName ?? string.Empty,
                    BillingCycles = dd.BillingCycles,
                    RentCharge = dd.ChargeAmount,
                    LabourCharge = dd.LabourCharge,
                    Total = dd.ChargeAmount + dd.LabourCharge,
                    // Money taken with the delivery is shown once, on its first line
                    CollectedAmount = i == 0 ? collected : 0m,
                });
            }
        }

        return response;
    }

    public async Task<BookingPaymentResultResponse> CreateBookingPaymentAsync(
        BookingPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CashAmount < 0 || request.AdvanceAmount < 0 || request.DiscountAmount < 0)
            throw new Exception("Amounts cannot be negative");
        var total = request.CashAmount + request.AdvanceAmount + request.DiscountAmount;
        if (total <= 0)
            throw new Exception("Enter a payment, advance or discount amount");
        if (request.DiscountAmount > 0 && string.IsNullOrWhiteSpace(request.DiscountReason))
            throw new Exception("A reason is required for a discount / adjustment");
        if (request.CashAmount > 0 && request.PaymentMethod != PaymentMethods.CASH && !request.BankId.HasValue)
            throw new Exception("Select the bank for a bank / cheque payment");

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null || customer.IsDeleted)
            throw new Exception("Customer not found");

        var ledger = await LoadBookingLedgerAsync(request.BookingId, cancellationToken);
        if (ledger.Booking.CustomerId != request.CustomerId)
            throw new Exception("The selected booking does not belong to this customer");

        var due = Math.Max(ledger.Summary.Due, 0m);
        if (total > due + 0.01m)
            throw new Exception($"Payment + advance + discount (৳{total:N2}) cannot be more than the booking due (৳{due:N2})");

        var advanceBalance = await GetAdvanceBalanceAsync(request.CustomerId, cancellationToken);
        if (request.AdvanceAmount > advanceBalance + 0.01m)
            throw new Exception($"Only ৳{advanceBalance:N2} advance is available");

        var result = new BookingPaymentResultResponse
        {
            ReceiptCode = request.TransactionCode,
            CashAmount = request.CashAmount,
            AdvanceApplied = request.AdvanceAmount,
            Discount = request.DiscountAmount,
            DueAfter = Math.Max(due - total, 0m),
        };
        var refPart = string.IsNullOrWhiteSpace(request.PaymentReference) ? "" : $" (Ref: {request.PaymentReference})";

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (request.CashAmount > 0)
            {
                var head = await GetOrCreateCustomerPaymentHeadAsync(cancellationToken);
                var payment = await AddCustomerPaymentTransactionAsync(
                    head, customer, request.TransactionCode, request.TransactionDate, request.BranchId,
                    request.CashAmount, request.PaymentMethod, request.PaymentReference, request.Note, request.BankId,
                    request.BookingId, $"Customer Payment - {customer.CustomerName} - Booking {ledger.Booking.BookingNumber}{refPart}",
                    cancellationToken);
                result.PaymentTransactionId = payment.Id;
            }

            if (request.AdvanceAmount > 0)
            {
                await AllocateAdvanceAsync(customer.Id, request.BookingId, request.AdvanceAmount,
                    request.TransactionDate, request.TransactionCode, request.Note, cancellationToken);
            }

            if (request.DiscountAmount > 0)
            {
                var discount = await CreateCustomerDiscountAsync(new CustomerDiscountRequest(
                    request.TransactionCode,
                    request.TransactionDate,
                    request.BranchId,
                    request.CustomerId,
                    request.DiscountAmount,
                    request.DiscountReason!,
                    TotalDue: due,
                    CurrentDue: result.DueAfter,
                    Note: request.Note,
                    BookingId: request.BookingId), cancellationToken);
                result.DiscountTransactionId = discount.Id;
            }

            await dbTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }

        return result;
    }

    public async Task<CustomerAdvancesResponse> GetCustomerAdvancesAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null || customer.IsDeleted)
            throw new Exception("Customer not found");

        var advances = await AdvancePaymentsQuery(customerId)
            .OrderBy(t => t.TransactionDate).ThenBy(t => t.CreatedTime)
            .Select(t => new { t.Id, t.TransactionCode, t.TransactionDate, t.PaymentMethod, t.Amount })
            .ToListAsync(cancellationToken);
        var advanceIds = advances.Select(a => a.Id).ToList();

        var allocations = await _advanceAllocationRepository.Query()
            .Where(a => advanceIds.Contains(a.AdvanceTransactionId))
            .OrderBy(a => a.AllocationDate).ThenBy(a => a.CreatedTime)
            .Select(a => new AdvanceAllocationLine
            {
                Id = a.Id,
                Date = a.AllocationDate,
                ReceiptCode = a.ReceiptCode,
                AdvanceCode = a.AdvanceTransaction!.TransactionCode,
                BookingNumber = a.Booking!.BookingNumber,
                Amount = a.Amount,
            })
            .ToListAsync(cancellationToken);
        var appliedByAdvance = await _advanceAllocationRepository.Query()
            .Where(a => advanceIds.Contains(a.AdvanceTransactionId))
            .GroupBy(a => a.AdvanceTransactionId)
            .Select(g => new { g.Key, Amount = g.Sum(a => a.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Amount, cancellationToken);

        var response = new CustomerAdvancesResponse
        {
            CustomerId = customer.Id,
            CustomerName = customer.CustomerName,
            Allocations = allocations,
        };
        foreach (var a in advances)
        {
            var applied = appliedByAdvance.GetValueOrDefault(a.Id);
            response.Advances.Add(new CustomerAdvanceLine
            {
                TransactionId = a.Id,
                TransactionCode = a.TransactionCode,
                Date = a.TransactionDate,
                PaymentMethod = a.PaymentMethod,
                Amount = a.Amount,
                Applied = applied,
                Remaining = a.Amount - applied,
            });
        }
        response.TotalReceived = response.Advances.Sum(a => a.Amount);
        response.TotalApplied = response.Advances.Sum(a => a.Applied);
        response.AdvanceBalance = response.TotalReceived - response.TotalApplied;
        return response;
    }

    public async Task<bool> RemoveAdvanceAllocationAsync(Guid allocationId, CancellationToken cancellationToken = default)
    {
        var allocation = await _advanceAllocationRepository.Query()
            .FirstOrDefaultAsync(a => a.Id == allocationId, cancellationToken)
            ?? throw new Exception("Advance allocation not found");

        await _editWindowGuard.EnsureEditableAsync(allocation.AllocationDate, "advance allocation", cancellationToken);

        // The amount simply goes back to the customer's advance balance
        await _advanceAllocationRepository.DeletableQuery(a => a.Id == allocationId).ExecuteDeleteAsync(cancellationToken);
        return true;
    }

    public async Task<PaymentReceiptInfoResponse> GetPaymentReceiptInfoAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var tx = await _transactionRepository.Query()
            .Where(t => t.Id == transactionId)
            .Select(t => new { t.TransactionCode, t.BookingId, t.Amount, BookingNumber = t.Booking != null ? t.Booking.BookingNumber : null })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new Exception("Transaction not found");

        var info = new PaymentReceiptInfoResponse { BookingNumber = tx.BookingNumber, CashAmount = tx.Amount };
        if (!tx.BookingId.HasValue) return info;

        var bookingId = tx.BookingId.Value;
        info.AdvanceApplied = await _advanceAllocationRepository.Query()
            .Where(a => a.BookingId == bookingId && a.ReceiptCode == tx.TransactionCode)
            .SumAsync(a => a.Amount, cancellationToken);
        info.Discount = await _transactionRepository.Query()
            .Where(t => t.BookingId == bookingId && t.TransactionCode == tx.TransactionCode
                        && t.TransactionHead != null && t.TransactionHead.UsageFor == UsageFor.BILL_DISCOUNT)
            .SumAsync(t => t.Amount > 0 ? t.Amount : t.DiscountAmount, cancellationToken);
        info.BookingDueNow = Math.Max((await LoadBookingLedgerAsync(bookingId, cancellationToken)).Summary.Due, 0m);
        return info;
    }

    /// <summary>Booking, its deliveries, payments, advance applications, discounts and the due summary.</summary>
    private async Task<BookingLedger> LoadBookingLedgerAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.Query()
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails).ThenInclude(bd => bd.Product)
            .Include(b => b.BookingDetails).ThenInclude(bd => bd.BookingUnit)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.TenantId == _tenantId, cancellationToken);
        if (booking == null || booking.IsDeleted)
            throw new Exception("Booking not found");
        if (booking.IsArchived)
            throw new Exception("This booking is archived; its balance was carried forward to the customer");

        var deliveries = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails).ThenInclude(dd => dd.BookingDetail).ThenInclude(bd => bd!.Product)
            .Include(d => d.DeliveryDetails).ThenInclude(dd => dd.DeliveryUnit)
            .Where(d => d.BookingId == bookingId && !d.IsArchived)
            .ToListAsync(cancellationToken);

        var deliveryIds = deliveries.Select(d => (Guid?)d.Id).ToList();
        var deliveryTxIds = deliveries.Where(d => d.TransactionId.HasValue).Select(d => d.TransactionId!.Value).ToList();

        var payments = await _transactionRepository.Query()
            .Where(t => !t.IsArchived && t.BookingId == bookingId
                        && t.TransactionHead != null
                        && t.TransactionHead.Type == TransactionHeadTypes.DEBIT
                        && t.TransactionHead.UsageFor == UsageFor.CUSTOMER_PAYMENT)
            .OrderBy(t => t.TransactionDate).ThenBy(t => t.CreatedTime)
            .Select(t => new { t.Id, t.TransactionDate, t.TransactionCode, t.PaymentMethod, t.Description, t.Amount, t.DeliveryId })
            .ToListAsync(cancellationToken);

        var discounts = await _transactionRepository.Query()
            .Where(t => !t.IsArchived && t.BookingId == bookingId
                        && t.TransactionHead != null
                        && t.TransactionHead.UsageFor == UsageFor.BILL_DISCOUNT)
            .OrderBy(t => t.TransactionDate)
            .Select(t => new BookingPaymentHistoryLine
            {
                Id = t.Id,
                Date = t.TransactionDate,
                ReferenceCode = t.TransactionCode,
                Kind = "DISCOUNT",
                PaymentMethod = t.PaymentMethod,
                Description = t.DiscountReason ?? t.Description,
                Amount = t.Amount > 0 ? t.Amount : t.DiscountAmount,
            })
            .ToListAsync(cancellationToken);

        var advanceApplications = await _advanceAllocationRepository.Query()
            .Where(a => a.BookingId == bookingId && !a.AdvanceTransaction!.IsArchived)
            .OrderBy(a => a.AllocationDate)
            .Select(a => new BookingPaymentHistoryLine
            {
                Id = a.Id,
                Date = a.AllocationDate,
                ReferenceCode = a.ReceiptCode ?? a.AdvanceTransaction!.TransactionCode,
                Kind = "ADVANCE",
                PaymentMethod = "ADVANCE",
                Description = "Advance " + a.AdvanceTransaction!.TransactionCode + " applied",
                Amount = a.Amount,
            })
            .ToListAsync(cancellationToken);

        await _ledgerService.SyncBookingAsync(bookingId, cancellationToken: cancellationToken);
        var charges = (await _ledgerService.GetChargesAsync([bookingId], DateTime.UtcNow, cancellationToken)).GetValueOrDefault(bookingId) ?? new LedgerCharges();
        var rent = charges.Rent;
        var labour = charges.Labour;
        var adjustment = charges.Adjustment;
        var accrued = charges.Total;

        var paymentLines = payments.Select(p =>
        {
            var atDelivery = (p.DeliveryId.HasValue && deliveryIds.Contains(p.DeliveryId)) || deliveryTxIds.Contains(p.Id);
            return new BookingPaymentHistoryLine
            {
                Id = p.Id,
                Date = p.TransactionDate,
                ReferenceCode = p.TransactionCode,
                Kind = atDelivery ? "AT_DELIVERY" : "PAYMENT",
                PaymentMethod = p.PaymentMethod,
                Description = p.Description,
                Amount = p.Amount,
            };
        }).ToList();

        var summary = new BookingPaymentSummary
        {
            RentCharge = rent,
            LabourCharge = labour,
            Adjustment = adjustment,
            TotalCharge = accrued,
            PaidAtDelivery = paymentLines.Where(p => p.Kind == "AT_DELIVERY").Sum(p => p.Amount),
            OtherPayments = paymentLines.Where(p => p.Kind == "PAYMENT").Sum(p => p.Amount),
            AdvanceApplied = advanceApplications.Sum(a => a.Amount),
            Discount = discounts.Sum(d => d.Amount),
        };
        summary.Due = summary.TotalCharge - summary.PaidAtDelivery - summary.OtherPayments - summary.AdvanceApplied - summary.Discount;

        // Money taken with each delivery (for the deliveries table)
        var collectedByDelivery = deliveries.ToDictionary(
            d => d.Id,
            d => payments.Where(x => x.DeliveryId == d.Id || (d.TransactionId.HasValue && x.Id == d.TransactionId.Value)).Sum(x => x.Amount));

        return new BookingLedger(booking, deliveries, paymentLines, advanceApplications, discounts, summary, collectedByDelivery);
    }

    private sealed record BookingLedger(
        Booking Booking,
        List<Delivery> Deliveries,
        List<BookingPaymentHistoryLine> Payments,
        List<BookingPaymentHistoryLine> AdvanceApplications,
        List<BookingPaymentHistoryLine> Discounts,
        BookingPaymentSummary Summary,
        Dictionary<Guid, decimal> CollectedByDelivery);

    /// <summary>Customer payments not tied to any booking: the customer's advances (including older unlinked payments).</summary>
    private IQueryable<Transaction> AdvancePaymentsQuery(int customerId)
        => _transactionRepository.Query()
            .Where(t => !t.IsArchived && t.CustomerId == customerId && !t.BookingId.HasValue
                        && t.TransactionHead != null
                        && t.TransactionHead.Type == TransactionHeadTypes.DEBIT
                        && t.TransactionHead.UsageFor == UsageFor.CUSTOMER_PAYMENT);

    private async Task<decimal> GetAdvanceBalanceAsync(int customerId, CancellationToken cancellationToken)
    {
        var received = await AdvancePaymentsQuery(customerId).SumAsync(t => t.Amount, cancellationToken);
        var advanceIds = AdvancePaymentsQuery(customerId).Select(t => t.Id);
        var applied = await _advanceAllocationRepository.Query()
            .Where(a => advanceIds.Contains(a.AdvanceTransactionId))
            .SumAsync(a => a.Amount, cancellationToken);
        return received - applied;
    }

    /// <summary>Applies advance to a booking, taking it from the oldest advances that still have a balance.</summary>
    private async Task AllocateAdvanceAsync(int customerId, Guid bookingId, decimal amount, DateTime date,
        string receiptCode, string? note, CancellationToken cancellationToken)
    {
        var advances = await AdvancePaymentsQuery(customerId)
            .OrderBy(t => t.TransactionDate).ThenBy(t => t.CreatedTime)
            .Select(t => new { t.Id, t.Amount })
            .ToListAsync(cancellationToken);
        var advanceIds = advances.Select(a => a.Id).ToList();
        var applied = await _advanceAllocationRepository.Query()
            .Where(a => advanceIds.Contains(a.AdvanceTransactionId))
            .GroupBy(a => a.AdvanceTransactionId)
            .Select(g => new { g.Key, Amount = g.Sum(a => a.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Amount, cancellationToken);

        var remaining = amount;
        foreach (var advance in advances)
        {
            if (remaining <= 0) break;
            var available = advance.Amount - applied.GetValueOrDefault(advance.Id);
            if (available <= 0) continue;

            var take = Math.Min(available, remaining);
            var allocation = new AdvanceAllocation
            {
                Id = Guid.NewGuid(),
                AdvanceTransactionId = advance.Id,
                BookingId = bookingId,
                CustomerId = customerId,
                Amount = take,
                AllocationDate = date,
                ReceiptCode = receiptCode,
                Note = note,
            };
            _defaultValueInjector.InjectCreatingAudit<AdvanceAllocation, Guid>(allocation);
            await _advanceAllocationRepository.AddAsync(allocation, cancellationToken);
            remaining -= take;
        }

        if (remaining > 0.01m)
            throw new Exception("Not enough advance available");
    }

    private async Task<Dictionary<Guid, decimal>> SumByBookingAsync(List<Guid> bookingIds, string usageFor, CancellationToken cancellationToken)
        => await _transactionRepository.Query()
            .Where(t => !t.IsArchived && t.BookingId.HasValue && bookingIds.Contains(t.BookingId.Value)
                        && t.TransactionHead != null && t.TransactionHead.UsageFor == usageFor
                        && (usageFor != UsageFor.CUSTOMER_PAYMENT || t.TransactionHead.Type == TransactionHeadTypes.DEBIT))
            .GroupBy(t => t.BookingId!.Value)
            .Select(g => new { g.Key, Amount = g.Sum(t => usageFor == UsageFor.BILL_DISCOUNT && t.Amount <= 0 ? t.DiscountAmount : t.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Amount, cancellationToken);

    private async Task<Dictionary<Guid, decimal>> SumAllocationsByBookingAsync(List<Guid> bookingIds, CancellationToken cancellationToken)
        => await _advanceAllocationRepository.Query()
            .Where(a => bookingIds.Contains(a.BookingId) && !a.AdvanceTransaction!.IsArchived)
            .GroupBy(a => a.BookingId)
            .Select(g => new { g.Key, Amount = g.Sum(a => a.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Amount, cancellationToken);

    private async Task<TransactionHead> GetOrCreateCustomerPaymentHeadAsync(CancellationToken cancellationToken)
    {
        var transactionHead = await _transactionHeadRepository.Query()
            .FirstOrDefaultAsync(x => x.UsageFor == UsageFor.CUSTOMER_PAYMENT && x.IsActive, cancellationToken);
        if (transactionHead != null) return transactionHead;

        transactionHead = new TransactionHead
        {
            Id = Guid.NewGuid(),
            Code = "CUSTOMER_PAYMENT",
            Name = "Customer Payment",
            Type = TransactionHeadTypes.DEBIT,
            DisplayType = "Debit",
            Description = "Direct payments received from customers",
            UsageFor = UsageFor.CUSTOMER_PAYMENT,
            IsSystem = true,
            IsActive = true,
            SortOrder = 1,
            ColorCode = "#10B981",
            IconClass = "fa-hand-holding-usd",
            TenantId = _tenantId
        };
        _defaultValueInjector.InjectCreatingAudit<TransactionHead, Guid>(transactionHead);
        await _transactionHeadRepository.AddAsync(transactionHead, cancellationToken);
        return transactionHead;
    }

    /// <summary>Adds one CUSTOMER_PAYMENT transaction and, for bank/cheque payments, its bank deposit.</summary>
    private async Task<Transaction> AddCustomerPaymentTransactionAsync(
        TransactionHead transactionHead,
        Customer customer,
        string transactionCode,
        DateTime transactionDate,
        int branchId,
        decimal amount,
        string paymentMethod,
        string? paymentReference,
        string? note,
        int? bankId,
        Guid? bookingId,
        string description,
        CancellationToken cancellationToken)
    {
        var entity = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionCode = transactionCode,
            TransactionDate = transactionDate,
            TransactionHeadId = transactionHead.Id,
            BranchId = branchId,
            CustomerId = customer.Id,
            BookingId = bookingId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            PaymentReference = paymentReference,
            BankId = bankId,
            Note = note,
            Description = description,
            DiscountAmount = 0,
            AdjustmentValue = 0,
            NetAmount = amount
        };

        _defaultValueInjector.InjectCreatingAudit<Transaction, Guid>(entity);
        await _transactionRepository.AddAsync(entity, cancellationToken);

        // If paid by Bank / Cheque, record deposit in BankTransaction
        if (paymentMethod != PaymentMethods.CASH && bankId.HasValue)
        {
            var bank = await _bankRepository.GetByIdAsync(bankId.Value, cancellationToken);
            if (bank == null || bank.IsDeleted)
                throw new Exception("Selected bank not found");

            var currentBalance = bank.OpeningBalance + await _bankTransactionRepository.Query()
                .Where(bt => bt.BankId == bankId.Value && bt.IsActive && !bt.IsDeleted)
                .SumAsync(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount, cancellationToken);

            var bankTx = new BankTransaction
            {
                TransactionNumber = transactionCode,
                TransactionDate = transactionDate,
                BankId = bankId.Value,
                TransactionType = BankTransactionTypes.Deposit,
                Amount = amount,
                Reference = paymentReference,
                Description = $"{description} ({paymentMethod})",
                BalanceAfter = currentBalance + amount,
                SourceType = BankSourceTypes.CUSTOMER_PAYMENT,
                TransactionId = entity.Id,
                BranchId = branchId,
                IsActive = true
            };
            _defaultValueInjector.InjectCreatingAudit<BankTransaction, long>(bankTx);
            await _bankTransactionRepository.AddAsync(bankTx, cancellationToken);
        }

        return entity;
    }

    #endregion

    public async Task<TransactionResponse> CreateDeliveryBillCollectionAsync(
        DeliveryBillCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Resolve customer ID
        int customerId = request.CustomerId ?? 0;
        if (customerId == 0 && request.DeliveryIds != null && request.DeliveryIds.Count > 0)
        {
            var firstDelivery = await _deliveryRepository.Query()
                .Include(d => d.Booking)
                .FirstOrDefaultAsync(d => request.DeliveryIds.Contains(d.Id), cancellationToken);
            if (firstDelivery?.Booking != null)
            {
                customerId = firstDelivery.Booking.CustomerId;
            }
        }

        var customerPaymentRequest = new CustomerPaymentRequest(
            request.TransactionCode,
            request.TransactionDate,
            request.BranchId,
            customerId,
            request.Amount,
            request.PaymentMethod,
            request.PaymentReference,
            request.Note,
            request.BankId,
            request.BookingId
        );

        return await CreateCustomerPaymentAsync(customerPaymentRequest, cancellationToken);
    }

    public async Task<List<CustomerPaymentReportItemResponse>> GetCustomerPaymentReportAsync(
        DateTime? startDate,
        DateTime? endDate,
        int? customerId,
        string? paymentMethod,
        CancellationToken cancellationToken = default)
    {
        var query = _transactionRepository.Query()
            .Include(t => t.Customer)
            .Include(t => t.Bank)
            .Include(t => t.TransactionHead)
            .Where(t => t.TenantId == _tenantId
                     && !t.IsDeleted
                     && !t.IsArchived
                     && t.TransactionHead != null
                     && t.TransactionHead.UsageFor == UsageFor.CUSTOMER_PAYMENT);

        if (startDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Local).ToUniversalTime();
            query = query.Where(t => t.TransactionDate >= fromUtc);
        }

        if (endDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();
            query = query.Where(t => t.TransactionDate < toUtc);
        }

        if (customerId.HasValue && customerId.Value > 0)
        {
            query = query.Where(t => t.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            query = query.Where(t => t.PaymentMethod == paymentMethod);
        }

        var results = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedTime)
            .Select(t => new CustomerPaymentReportItemResponse(
                t.Id,
                t.TransactionCode,
                t.TransactionDate,
                t.CustomerId ?? 0,
                t.Customer != null ? t.Customer.CustomerName : "Unknown",
                t.Customer != null ? t.Customer.CustomerMobile : null,
                t.Customer != null ? t.Customer.Address : null,
                t.Amount,
                t.PaymentMethod,
                t.Bank != null ? t.Bank.BankName : null,
                t.PaymentReference,
                t.Note,
                t.CreatedTime
            ))
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task<TransactionResponse> CreateCustomerDiscountAsync(
        CustomerDiscountRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CustomerId <= 0)
            throw new Exception("Please select a valid customer");

        if (request.DiscountAmount <= 0)
            throw new Exception("Discount / adjustment amount must be greater than zero");

        if (string.IsNullOrWhiteSpace(request.DiscountReason))
            throw new Exception("A valid reason or approval justification is required for discount/adjustment");

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null || customer.IsDeleted)
            throw new Exception("Customer not found");

        var summary = await GetCustomerBalanceSummaryAsync(request.CustomerId, cancellationToken);
        // Booking discounts are checked against the booking due by the booking payment; others against the customer due
        if (!request.BookingId.HasValue && summary.NetDue > 0 && request.DiscountAmount > summary.NetDue)
            throw new Exception($"Discount amount cannot be greater than Total Due (৳{summary.NetDue:N2})");

        var discountHead = await _transactionHeadRepository.Query()
            .FirstOrDefaultAsync(x => x.UsageFor == UsageFor.BILL_DISCOUNT && x.IsActive, cancellationToken);

        if (discountHead == null)
        {
            discountHead = new TransactionHead
            {
                Id = Guid.NewGuid(),
                Code = "BILL_DISCOUNT",
                Name = "Bill Discount / Adjustment",
                Type = TransactionHeadTypes.CREDIT,
                DisplayType = "Credit",
                Description = "Discounts and approved write-offs reducing customer bills",
                UsageFor = UsageFor.BILL_DISCOUNT,
                IsSystem = true,
                IsActive = true,
                SortOrder = 2,
                ColorCode = "#F59E0B",
                IconClass = "fa-tag",
                TenantId = _tenantId
            };
            _defaultValueInjector.InjectCreatingAudit<TransactionHead, Guid>(discountHead);
            await _transactionHeadRepository.AddAsync(discountHead, cancellationToken);
        }

        var totalDue = request.TotalDue ?? summary.NetDue;
        var currentDue = request.CurrentDue ?? Math.Max(0m, totalDue - request.DiscountAmount);
        var reasonPart = request.DiscountReason.Trim();
        var entity = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionCode = request.TransactionCode,
            TransactionDate = request.TransactionDate,
            TransactionHeadId = discountHead.Id,
            BranchId = request.BranchId,
            CustomerId = request.CustomerId,
            BookingId = request.BookingId,
            Amount = request.DiscountAmount,
            PaymentMethod = PaymentMethods.DISCOUNT,
            PaymentReference = null,
            BankId = null,
            Note = request.Note,
            DiscountReason = reasonPart,
            Description = $"{customer.CustomerName} [Total Due: ৳{totalDue:N2}, Discount: ৳{request.DiscountAmount:N2}, Current Due: ৳{currentDue:N2}] ({reasonPart})",
            DiscountAmount = request.DiscountAmount,
            AdjustmentValue = 0,
            TotalDue = totalDue,
            CurrentDue = currentDue,
            NetAmount = 0 // Zero physical cash impact
        };

        _defaultValueInjector.InjectCreatingAudit<Transaction, Guid>(entity);
        await _transactionRepository.AddAsync(entity, cancellationToken);

        return entity.Adapt<TransactionResponse>();
    }

    public async Task<List<CustomerDiscountItemResponse>> GetCustomerDiscountHistoryAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var discounts = await _transactionRepository.Query()
            .Include(t => t.Customer)
            .Include(t => t.TransactionHead)
            .Where(t => t.TenantId == _tenantId
                     && !t.IsDeleted
                     && !t.IsArchived
                     && t.CustomerId == customerId
                     && t.TransactionHead != null
                     && t.TransactionHead.UsageFor == UsageFor.BILL_DISCOUNT)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedTime)
            .Select(t => new CustomerDiscountItemResponse(
                t.Id,
                t.TransactionCode,
                t.TransactionDate,
                t.CustomerId ?? 0,
                t.Customer != null ? t.Customer.CustomerName : "Unknown",
                t.Customer != null ? t.Customer.CustomerMobile : null,
                t.Customer != null ? t.Customer.Address : null,
                t.TotalDue ?? (t.Amount > 0 ? t.Amount : t.DiscountAmount),
                t.Amount > 0 ? t.Amount : t.DiscountAmount,
                t.CurrentDue ?? 0m,
                t.DiscountReason ?? string.Empty,
                t.Note,
                t.BookingId,
                t.CreatedTime
            ))
            .ToListAsync(cancellationToken);

        return discounts;
    }

    public async Task<List<CustomerDiscountItemResponse>> GetCustomerDiscountReportAsync(
        DateTime? startDate,
        DateTime? endDate,
        int? customerId,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = _transactionRepository.Query()
            .Include(t => t.Customer)
            .Include(t => t.TransactionHead)
            .Where(t => t.TenantId == _tenantId
                     && !t.IsDeleted
                     && !t.IsArchived
                     && t.TransactionHead != null
                     && t.TransactionHead.UsageFor == UsageFor.BILL_DISCOUNT);

        if (startDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Local).ToUniversalTime();
            query = query.Where(t => t.TransactionDate >= fromUtc);
        }

        if (endDate.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();
            query = query.Where(t => t.TransactionDate < toUtc);
        }

        if (customerId.HasValue && customerId.Value > 0)
        {
            query = query.Where(t => t.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(t => t.TransactionCode.ToLower().Contains(term)
                                  || (t.Customer != null && t.Customer.CustomerName.ToLower().Contains(term))
                                  || (t.Customer != null && t.Customer.CustomerMobile != null && t.Customer.CustomerMobile.Contains(term))
                                  || (t.DiscountReason != null && t.DiscountReason.ToLower().Contains(term))
                                  || (t.Note != null && t.Note.ToLower().Contains(term)));
        }

        var results = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedTime)
            .Select(t => new CustomerDiscountItemResponse(
                t.Id,
                t.TransactionCode,
                t.TransactionDate,
                t.CustomerId ?? 0,
                t.Customer != null ? t.Customer.CustomerName : "Unknown",
                t.Customer != null ? t.Customer.CustomerMobile : null,
                t.Customer != null ? t.Customer.Address : null,
                t.TotalDue ?? (t.Amount > 0 ? t.Amount : t.DiscountAmount),
                t.Amount > 0 ? t.Amount : t.DiscountAmount,
                t.CurrentDue ?? 0m,
                t.DiscountReason ?? string.Empty,
                t.Note,
                t.BookingId,
                t.CreatedTime
            ))
            .ToListAsync(cancellationToken);

        return results;
    }
}
