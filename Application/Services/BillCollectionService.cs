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

    public async Task<IEnumerable<Lookup<Guid>>> GetBookingsWithDueAsync(CancellationToken cancellationToken = default)
    {
        var bookings = await _bookingRepository.Query()
            .Where(b => b.TenantId == _tenantId)
            .Include(b => b.BookingDetails)
            .ToListAsync(cancellationToken);

        var bookingIds = bookings.Select(b => b.Id).ToList();
        
        // Fetch bulk deliveries and group by bookingId
        var deliveriesGrouped = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails)
            .Where(d => bookingIds.Contains(d.BookingId) && d.TenantId == _tenantId && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        var deliveriesByBooking = deliveriesGrouped
            .GroupBy(d => d.BookingId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Fetch bulk paid amounts and group by bookingId
        var paidAmountsMap = await _transactionRepository.Query()
            .Where(t => t.BookingId != null && bookingIds.Contains(t.BookingId.Value) &&
                        !t.IsDeleted &&
                        t.TransactionHead!.Type == TransactionHeadTypes.DEBIT &&
                        (t.TransactionHead!.UsageFor == UsageFor.BILL_COLLECTION ||
                         t.TransactionHead!.UsageFor == UsageFor.LABOUR_CHARGE))
            .GroupBy(t => t.BookingId!.Value)
            .Select(g => new { BookingId = g.Key, PaidAmount = g.Sum(t => t.NetAmount) })
            .ToDictionaryAsync(x => x.BookingId, x => x.PaidAmount, cancellationToken);

        var bookingsWithDue = new List<Lookup<Guid>>();
        var now = DateTime.UtcNow;

        foreach (var booking in bookings)
        {
            var activeDetails = booking.BookingDetails.Where(d => !d.IsDeleted).ToList();
            var bDeliveries = deliveriesByBooking.TryGetValue(booking.Id, out var bdList) ? bdList : new List<Delivery>();
            var paidAmount = paidAmountsMap.GetValueOrDefault(booking.Id, 0m);
            
            var (_, _, totalAccrued, _) = Application.Services.Common.BookingDueCalculator.CalculateBookingAccruedDetails(
                booking,
                activeDetails,
                bDeliveries,
                now);

            var dueAmount = totalAccrued - paidAmount;

            // Only include bookings with due amount > 0
            if (dueAmount > 0)
            {
                bookingsWithDue.Add(new Lookup<Guid>(booking.Id, booking.BookingNumber));
            }
        }

        return bookingsWithDue;
    }

    public async Task<BookingWithDueResponse?> GetBookingForBillCollectionAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.Query()
            .Where(b => b.Id == bookingId && b.TenantId == _tenantId)
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails)
            .FirstOrDefaultAsync(cancellationToken);

        if (booking == null)
            return null;

        var totalAmount = await GetBookingTotalAmountAsync(bookingId, cancellationToken);
        var paidAmount = await GetBookingPaidAmountAsync(bookingId, cancellationToken);
        var dueAmount = totalAmount - paidAmount;

        // Get last delivery date
        var lastDeliveryDate = await _deliveryRepository.Query()
            .Where(d => d.BookingId == bookingId && d.TenantId == _tenantId)
            .OrderByDescending(d => d.DeliveryDate)
            .Select(d => (DateTime?)d.DeliveryDate)
            .FirstOrDefaultAsync(cancellationToken);

        return new BookingWithDueResponse(
            BookingId: booking.Id,
            BookingNumber: booking.BookingNumber,
            BookingDate: booking.BookingDate,
            CustomerId: booking.CustomerId,
            CustomerName: booking.Customer?.CustomerName ?? "N/A",
            LastDeliveryDate: lastDeliveryDate,
            TotalAmount: totalAmount,
            PaidAmount: paidAmount,
            DueAmount: dueAmount
        );
    }

    public async Task<decimal> GetBookingTotalAmountAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.Query()
            .Include(b => b.BookingDetails)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.TenantId == _tenantId, cancellationToken);

        if (booking == null) return 0m;

        var activeDetails = booking.BookingDetails.Where(d => !d.IsDeleted).ToList();

        var deliveries = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails)
            .Where(d => d.BookingId == bookingId && d.TenantId == _tenantId && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        var (_, _, totalAccrued, _) = Application.Services.Common.BookingDueCalculator.CalculateBookingAccruedDetails(
            booking,
            activeDetails,
            deliveries,
            DateTime.UtcNow);

        return totalAccrued;
    }

    public async Task<decimal> GetBookingPaidAmountAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var paidAmount = await _transactionRepository.Query()
            .Where(t => t.BookingId == bookingId &&
                       !t.IsDeleted &&
                       t.TransactionHead!.Type == TransactionHeadTypes.DEBIT &&
                       (t.TransactionHead!.UsageFor == UsageFor.BILL_COLLECTION ||
                        t.TransactionHead!.UsageFor == UsageFor.LABOUR_CHARGE))
            .SumAsync(t => t.NetAmount, cancellationToken);

        return paidAmount;
    }

    public async Task<TransactionResponse> CreateBillCollectionAsync(BillCollectionRequest request, CancellationToken cancellationToken = default)
    {
        // Get BILL_COLLECTION transaction head
        var transactionHead = await _transactionHeadRepository.Query()
            .FirstOrDefaultAsync(x => x.UsageFor == UsageFor.BILL_COLLECTION && x.IsActive, cancellationToken);

        if (transactionHead == null)
            throw new Exception("BILL_COLLECTION transaction head not found");

        // Get booking and customer info
        var booking = await _bookingRepository.Query()
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking == null)
            throw new Exception("Booking not found");

        // Create transaction entity
        var entity = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionCode = request.TransactionCode,
            TransactionDate = request.TransactionDate,
            TransactionHeadId = transactionHead.Id,
            BranchId = request.BranchId,
            BookingId = request.BookingId,
            CustomerId = booking.CustomerId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            PaymentReference = request.PaymentReference,
            BankId = request.BankId,
            Note = request.Note,
            Description = $"Bill Collection - {booking.BookingNumber} - {booking.Customer?.CustomerName}",
            DiscountAmount = 0,
            AdjustmentValue = 0,
            NetAmount = request.Amount
        };

        _defaultValueInjector.InjectCreatingAudit<Transaction, Guid>(entity);
        await _transactionRepository.AddAsync(entity, cancellationToken);

        // If paid by bank or cheque, automatically record deposit in BankTransaction
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
                    Description = $"Bill Collection - {booking.BookingNumber} - {booking.Customer?.CustomerName} ({request.PaymentMethod})",
                    BalanceAfter = currentBalance + request.Amount,
                    SourceType = BankSourceTypes.BILL_COLLECTION,
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

    public async Task<TransactionResponse> UpdateBillCollectionAsync(Guid id, BillCollectionRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _transactionRepository.Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
            throw new Exception("Transaction not found");

        // Get BILL_COLLECTION transaction head
        var transactionHead = await _transactionHeadRepository.Query()
            .FirstOrDefaultAsync(x => x.UsageFor == UsageFor.BILL_COLLECTION && x.IsActive, cancellationToken);

        if (transactionHead == null)
            throw new Exception("BILL_COLLECTION transaction head not found");

        // Get booking and customer info
        var booking = await _bookingRepository.Query()
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking == null)
            throw new Exception("Booking not found");

        // Update entity
        entity.TransactionCode = request.TransactionCode;
        entity.TransactionDate = request.TransactionDate;
        entity.TransactionHeadId = transactionHead.Id;
        entity.BranchId = request.BranchId;
        entity.BookingId = request.BookingId;
        entity.CustomerId = booking.CustomerId;
        entity.Amount = request.Amount;
        entity.PaymentMethod = request.PaymentMethod;
        entity.PaymentReference = request.PaymentReference;
        entity.BankId = request.BankId;
        entity.Note = request.Note;
        entity.Description = $"Bill Collection - {booking.BookingNumber} - {booking.Customer?.CustomerName}";
        entity.NetAmount = request.Amount;

        _defaultValueInjector.InjectUpdatingAudit<Transaction, Guid>(entity);
        await _transactionRepository.UpdateAsync(entity, cancellationToken);

        var response = entity.Adapt<TransactionResponse>();
        return response;
    }

    public async Task<TransactionResponse> CreateDeliveryBillCollectionAsync(DeliveryBillCollectionRequest request, CancellationToken cancellationToken = default)
    {
        // Get BILL_COLLECTION transaction head
        var transactionHead = await _transactionHeadRepository.Query()
            .FirstOrDefaultAsync(x => x.UsageFor == UsageFor.BILL_COLLECTION && x.IsActive, cancellationToken);

        if (transactionHead == null)
            throw new Exception("BILL_COLLECTION transaction head not found");

        // Get LABOUR_CHARGE transaction head
        var labourChargeHead = await _transactionHeadRepository.Query()
            .FirstOrDefaultAsync(x => x.UsageFor == UsageFor.LABOUR_CHARGE && x.IsActive, cancellationToken);

        if (labourChargeHead == null)
            throw new Exception("LABOUR_CHARGE transaction head not found");

        Transaction entity;
        var hasDeliveries = request.DeliveryIds != null && request.DeliveryIds.Count > 0;

        if (hasDeliveries)
        {
            // Get deliveries and verify they are all unpaid
            var deliveries = await _deliveryRepository.Query()
                .Include(d => d.Booking)
                .ThenInclude(b => b!.Customer)
                .Include(d => d.DeliveryDetails)
                .Where(d => request.DeliveryIds!.Contains(d.Id) && d.PaymentStatus == PaymentStatuses.UNPAID)
                .ToListAsync(cancellationToken);

            if (deliveries.Count != request.DeliveryIds!.Count)
                throw new Exception("Some deliveries are not found or already paid");

            // Query existing payments for these deliveries to account for prior payments (e.g. paid during delivery)
            var deliveryIds = deliveries.Select(d => d.Id).ToList();
            var deliveryTxIds = deliveries.Where(d => d.TransactionId.HasValue).Select(d => d.TransactionId!.Value).ToList();

            var existingPayments = await _transactionRepository.Query()
                .Include(t => t.TransactionHead)
                .Where(t => !t.IsDeleted
                         && t.TransactionHead != null
                         && (t.TransactionHead.UsageFor == UsageFor.BILL_COLLECTION || t.TransactionHead.UsageFor == UsageFor.LABOUR_CHARGE)
                         && ((t.DeliveryId.HasValue && deliveryIds.Contains(t.DeliveryId.Value)) || deliveryTxIds.Contains(t.Id)))
                .Select(t => new { t.Id, t.DeliveryId, t.Amount, UsageFor = t.TransactionHead!.UsageFor })
                .ToListAsync(cancellationToken);

            decimal remainingStorageCharges = 0m;
            decimal remainingLabourCharges = 0m;

            foreach (var delivery in deliveries)
            {
                var delStorageCharge = delivery.DeliveryDetails.Sum(dd => dd.ChargeAmount) + delivery.AdjustmentValue;
                var delLabourCharge = delivery.DeliveryDetails.Sum(dd => dd.LabourCharge);
                var delTotalBill = delStorageCharge + delLabourCharge;

                var delStoragePaid = existingPayments
                    .Where(p => (p.DeliveryId.HasValue && p.DeliveryId.Value == delivery.Id) || (delivery.TransactionId.HasValue && p.Id == delivery.TransactionId.Value))
                    .Where(p => p.UsageFor == UsageFor.BILL_COLLECTION)
                    .Sum(p => p.Amount);

                var delLabourPaid = existingPayments
                    .Where(p => (p.DeliveryId.HasValue && p.DeliveryId.Value == delivery.Id) || (delivery.TransactionId.HasValue && p.Id == delivery.TransactionId.Value))
                    .Where(p => p.UsageFor == UsageFor.LABOUR_CHARGE)
                    .Sum(p => p.Amount);

                var delTotalPaid = delStoragePaid + delLabourPaid;
                var delDue = Math.Max(0m, delTotalBill - delTotalPaid);

                var remLabour = Math.Max(0m, delLabourCharge - delLabourPaid);
                var remStorage = Math.Max(0m, delStorageCharge - delStoragePaid);

                if (remStorage + remLabour > delDue)
                {
                    remLabour = Math.Min(remLabour, delDue);
                    remStorage = delDue - remLabour;
                }

                remainingStorageCharges += remStorage;
                remainingLabourCharges += remLabour;
            }

            var deliveryGrandDue = remainingStorageCharges + remainingLabourCharges;

            if (request.Amount < deliveryGrandDue - 0.01m)
                throw new Exception($"Payment amount ({request.Amount:N2}) cannot be less than remaining delivery dues ({deliveryGrandDue:N2})");

            var excessAdvance = Math.Max(0m, request.Amount - deliveryGrandDue);

            var deliveryCodes = string.Join(", ", deliveries.Select(d => d.DeliveryNumber));
            var firstDelivery = deliveries.FirstOrDefault();
            var customerId = firstDelivery?.Booking?.CustomerId ?? request.CustomerId;
            var bookingId = firstDelivery?.BookingId ?? request.BookingId;

            var customer = customerId.HasValue
                ? await _customerRepository.GetByIdAsync(customerId.Value, cancellationToken)
                : null;
            var customerName = customer?.CustomerName ?? firstDelivery?.Booking?.Customer?.CustomerName ?? "Customer";

            var mainAmount = remainingStorageCharges + excessAdvance;
            var description = excessAdvance > 0
                ? $"Bill Collection - Deliveries: {deliveryCodes} (Advance: {excessAdvance:N2}) - {customerName}"
                : $"Bill Collection - Deliveries: {deliveryCodes} - {customerName}";

            entity = new Transaction
            {
                Id = Guid.NewGuid(),
                TransactionCode = request.TransactionCode,
                TransactionDate = request.TransactionDate,
                TransactionHeadId = transactionHead.Id,
                BranchId = request.BranchId,
                BookingId = bookingId,
                CustomerId = customerId,
                Amount = mainAmount,
                PaymentMethod = request.PaymentMethod,
                PaymentReference = request.PaymentReference,
                BankId = request.BankId,
                Note = request.Note,
                Description = description,
                DiscountAmount = 0,
                AdjustmentValue = 0,
                NetAmount = mainAmount
            };

            _defaultValueInjector.InjectCreatingAudit<Transaction, Guid>(entity);
            await _transactionRepository.AddAsync(entity, cancellationToken);

            // Create separate transaction for remaining unpaid labour charges if any
            if (remainingLabourCharges > 0)
            {
                var labourEntity = new Transaction
                {
                    Id = Guid.NewGuid(),
                    TransactionCode = request.TransactionCode + "-L",
                    TransactionDate = request.TransactionDate,
                    TransactionHeadId = labourChargeHead.Id,
                    BranchId = request.BranchId,
                    BookingId = bookingId,
                    CustomerId = customerId,
                    Amount = remainingLabourCharges,
                    PaymentMethod = request.PaymentMethod,
                    PaymentReference = request.PaymentReference,
                    BankId = request.BankId,
                    Note = request.Note,
                    Description = $"Labour Charge - Deliveries: {deliveryCodes} - {customerName}",
                    DiscountAmount = 0,
                    AdjustmentValue = 0,
                    NetAmount = remainingLabourCharges
                };

                _defaultValueInjector.InjectCreatingAudit<Transaction, Guid>(labourEntity);
                await _transactionRepository.AddAsync(labourEntity, cancellationToken);
            }

            // Mark all deliveries as paid
            foreach (var delivery in deliveries)
            {
                delivery.PaymentStatus = PaymentStatuses.PAID;
                delivery.PaymentDate = request.TransactionDate;
                delivery.TransactionId = entity.Id;
                _defaultValueInjector.InjectUpdatingAudit<Delivery, Guid>(delivery);
                await _deliveryRepository.UpdateAsync(delivery, cancellationToken);
            }
        }
        else
        {
            // Pure advance payment (no delivery selected)
            if (!request.CustomerId.HasValue)
                throw new Exception("Please select a customer for advance payment");

            if (request.Amount <= 0)
                throw new Exception("Advance payment amount must be greater than zero");

            var customer = await _customerRepository.GetByIdAsync(request.CustomerId.Value, cancellationToken);
            var customerName = customer?.CustomerName ?? "Customer";

            var bookingInfo = "";
            if (request.BookingId.HasValue)
            {
                var booking = await _bookingRepository.GetByIdAsync(request.BookingId.Value, cancellationToken);
                if (booking != null)
                {
                    bookingInfo = $" - Booking: {booking.BookingNumber}";
                }
            }

            entity = new Transaction
            {
                Id = Guid.NewGuid(),
                TransactionCode = request.TransactionCode,
                TransactionDate = request.TransactionDate,
                TransactionHeadId = transactionHead.Id,
                BranchId = request.BranchId,
                BookingId = request.BookingId,
                CustomerId = request.CustomerId,
                Amount = request.Amount,
                PaymentMethod = request.PaymentMethod,
                PaymentReference = request.PaymentReference,
                BankId = request.BankId,
                Note = request.Note,
                Description = $"Customer Advance Payment - {customerName}{bookingInfo}",
                DiscountAmount = 0,
                AdjustmentValue = 0,
                NetAmount = request.Amount
            };

            _defaultValueInjector.InjectCreatingAudit<Transaction, Guid>(entity);
            await _transactionRepository.AddAsync(entity, cancellationToken);
        }

        // If paid by bank or cheque, automatically record deposit in BankTransaction for grand total
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
                    Description = $"Delivery Bill Collection - {request.TransactionCode} ({request.PaymentMethod})",
                    BalanceAfter = currentBalance + request.Amount,
                    SourceType = BankSourceTypes.BILL_COLLECTION,
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
}
