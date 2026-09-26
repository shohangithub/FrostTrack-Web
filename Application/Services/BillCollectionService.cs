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
        ITenantProvider tenantProvider)
    {
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

        var now = DateTime.UtcNow;
        decimal totalBookingCharges = 0m;
        decimal totalDeliveryCharges = 0m;
        decimal totalRecurringCharges = 0m;

        foreach (var booking in bookings)
        {
            var activeDetails = booking.BookingDetails.Where(d => !d.IsDeleted).ToList();
            var bDeliveries = deliveriesByBooking.TryGetValue(booking.Id, out var bdList) ? bdList : new List<Delivery>();

            var (bTotalRent, bTotalLabour, bTotalAccrued, pendingRecurringCharge) =
                BookingDueCalculator.CalculateBookingAccruedDetails(booking, activeDetails, bDeliveries, now);

            var bDeliveryRent = bDeliveries.Sum(d => d.DeliveryDetails.Sum(dd => dd.ChargeAmount));
            var bDeliveryLabour = bDeliveries.Sum(d => d.DeliveryDetails.Sum(dd => dd.LabourCharge));
            var bDeliveryAdj = bDeliveries.Sum(d => d.AdjustmentValue);
            var bDeliveryCharges = bDeliveryRent + bDeliveryLabour + bDeliveryAdj;

            totalDeliveryCharges += bDeliveryCharges;
            totalRecurringCharges += pendingRecurringCharge;
            totalBookingCharges += Math.Max(0m, bTotalAccrued - bDeliveryCharges - pendingRecurringCharge);
        }

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
        if (customer == null)
            throw new Exception("Customer not found");

        // Ensure CUSTOMER_PAYMENT TransactionHead exists
        var transactionHead = await _transactionHeadRepository.Query()
            .FirstOrDefaultAsync(x => x.UsageFor == UsageFor.CUSTOMER_PAYMENT && x.IsActive, cancellationToken);

        if (transactionHead == null)
        {
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
        }

        var refPart = string.IsNullOrWhiteSpace(request.PaymentReference) ? "" : $" (Ref: {request.PaymentReference})";
        var entity = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionCode = request.TransactionCode,
            TransactionDate = request.TransactionDate,
            TransactionHeadId = transactionHead.Id,
            BranchId = request.BranchId,
            CustomerId = request.CustomerId,
            BookingId = request.BookingId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            PaymentReference = request.PaymentReference,
            BankId = request.BankId,
            Note = request.Note,
            Description = $"Customer Payment - {customer.CustomerName}{refPart}",
            DiscountAmount = 0,
            AdjustmentValue = 0,
            NetAmount = request.Amount
        };

        _defaultValueInjector.InjectCreatingAudit<Transaction, Guid>(entity);
        await _transactionRepository.AddAsync(entity, cancellationToken);

        // If paid by Bank / Cheque, record deposit in BankTransaction
        if (request.PaymentMethod != PaymentMethods.CASH && request.BankId.HasValue)
        {
            var bank = await _bankRepository.GetByIdAsync(request.BankId.Value, cancellationToken);
            if (bank != null)
            {
                var currentBalance = bank.OpeningBalance + await _bankTransactionRepository.Query()
                    .Where(bt => bt.BankId == request.BankId.Value && bt.IsActive && !bt.IsDeleted)
                    .SumAsync(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount, cancellationToken);

                var bankTx = new BankTransaction
                {
                    TransactionNumber = request.TransactionCode,
                    TransactionDate = request.TransactionDate,
                    BankId = request.BankId.Value,
                    TransactionType = BankTransactionTypes.Deposit,
                    Amount = request.Amount,
                    Reference = request.PaymentReference,
                    Description = $"Customer Payment - {request.TransactionCode} ({request.PaymentMethod}) - {customer.CustomerName}",
                    BalanceAfter = currentBalance + request.Amount,
                    SourceType = BankSourceTypes.CUSTOMER_PAYMENT,
                    TransactionId = entity.Id,
                    BranchId = request.BranchId,
                    IsActive = true
                };
                _defaultValueInjector.InjectCreatingAudit<BankTransaction, long>(bankTx);
                await _bankTransactionRepository.AddAsync(bankTx, cancellationToken);
            }
        }

        var response = entity.Adapt<TransactionResponse>();
        return response;
    }

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
        if (customer == null)
            throw new Exception("Customer not found");

        var summary = await GetCustomerBalanceSummaryAsync(request.CustomerId, cancellationToken);
        if (summary.NetDue > 0 && request.DiscountAmount > summary.NetDue)
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
