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
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly Guid _tenantId;

    public BillCollectionService(
        IRepository<Booking, Guid> bookingRepository,
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<TransactionHead, Guid> transactionHeadRepository,
        IRepository<Delivery, Guid> deliveryRepository,
        DefaultValueInjector defaultValueInjector,
        ITenantProvider tenantProvider)
    {
        _bookingRepository = bookingRepository;
        _transactionRepository = transactionRepository;
        _transactionHeadRepository = transactionHeadRepository;
        _deliveryRepository = deliveryRepository;
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

        var deliveryChargeMap = deliveriesGrouped
            .GroupBy(d => d.BookingId)
            .ToDictionary(
                g => g.Key, 
                g => g.Sum(d => d.ChargeAmount + d.AdjustmentValue + (d.DeliveryDetails?.Sum(dd => dd.LabourCharge) ?? 0m))
            );

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
            var deliveryCharge = deliveryChargeMap.GetValueOrDefault(booking.Id, 0m);
            var paidAmount = paidAmountsMap.GetValueOrDefault(booking.Id, 0m);
            
            var (totalAccrued, _) = Application.Services.Common.BookingDueCalculator.CalculateBookingAccruedAmount(
                booking,
                activeDetails,
                deliveryCharge,
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

        var deliveryCharge = deliveries.Sum(d => d.ChargeAmount + d.AdjustmentValue + (d.DeliveryDetails?.Sum(dd => dd.LabourCharge) ?? 0m));

        var (totalAccrued, _) = Application.Services.Common.BookingDueCalculator.CalculateBookingAccruedAmount(
            booking,
            activeDetails,
            deliveryCharge,
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
            Note = request.Note,
            Description = $"Bill Collection - {booking.BookingNumber} - {booking.Customer?.CustomerName}",
            DiscountAmount = 0,
            AdjustmentValue = 0,
            NetAmount = request.Amount
        };

        _defaultValueInjector.InjectCreatingAudit<Transaction, Guid>(entity);
        await _transactionRepository.AddAsync(entity, cancellationToken);

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

        // Get deliveries and verify they are all unpaid
        var deliveries = await _deliveryRepository.Query()
            .Include(d => d.Booking)
            .ThenInclude(b => b!.Customer)
            .Include(d => d.DeliveryDetails)
            .Where(d => request.DeliveryIds.Contains(d.Id) && d.PaymentStatus == PaymentStatuses.UNPAID)
            .ToListAsync(cancellationToken);

        if (deliveries.Count != request.DeliveryIds.Count)
            throw new Exception("Some deliveries are not found or already paid");

        // Calculate total charges including labour
        var totalCharges = deliveries.Sum(d => d.DeliveryDetails.Sum(dd => dd.ChargeAmount + dd.AdjustmentValue));
        var totalLabourCharges = deliveries.Sum(d => d.DeliveryDetails.Sum(dd => dd.LabourCharge));
        var grandTotal = totalCharges + totalLabourCharges;

        if (Math.Abs(grandTotal - request.Amount) > 0.01m)
            throw new Exception($"Payment amount ({request.Amount}) does not match total delivery charges ({grandTotal})");

        // Create main transaction for delivery charges
        var deliveryCodes = string.Join(", ", deliveries.Select(d => d.DeliveryNumber));
        var firstDelivery = deliveries.FirstOrDefault();
        var customer = await _bookingRepository.Query()
            .Where(b => b.Id == firstDelivery!.BookingId)
            .Include(b => b.Customer)
            .Select(b => b.Customer)
            .FirstOrDefaultAsync(cancellationToken);
        var customerName = customer?.CustomerName ?? "N/A";

        var entity = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionCode = request.TransactionCode,
            TransactionDate = request.TransactionDate,
            TransactionHeadId = transactionHead.Id,
            BranchId = request.BranchId,
            BookingId = deliveries.FirstOrDefault()?.BookingId,
            CustomerId = deliveries.FirstOrDefault()?.Booking?.CustomerId,
            Amount = totalCharges,
            PaymentMethod = request.PaymentMethod,
            PaymentReference = request.PaymentReference,
            Note = request.Note,
            Description = $"Bill Collection - Deliveries: {deliveryCodes} - {customerName}",
            DiscountAmount = 0,
            AdjustmentValue = 0,
            NetAmount = totalCharges
        };

        _defaultValueInjector.InjectCreatingAudit<Transaction, Guid>(entity);
        await _transactionRepository.AddAsync(entity, cancellationToken);

        // Create separate transaction for labour charges if any
        if (totalLabourCharges > 0)
        {
            var labourEntity = new Transaction
            {
                Id = Guid.NewGuid(),
                TransactionCode = request.TransactionCode + "-L",
                TransactionDate = request.TransactionDate,
                TransactionHeadId = labourChargeHead.Id,
                BranchId = request.BranchId,
                BookingId = deliveries.FirstOrDefault()?.BookingId,
                CustomerId = deliveries.FirstOrDefault()?.Booking?.CustomerId,
                Amount = totalLabourCharges,
                PaymentMethod = request.PaymentMethod,
                PaymentReference = request.PaymentReference,
                Note = request.Note,
                Description = $"Labour Charge - Deliveries: {deliveryCodes} - {customerName}",
                DiscountAmount = 0,
                AdjustmentValue = 0,
                NetAmount = totalLabourCharges
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

        var response = entity.Adapt<TransactionResponse>();
        return response;
    }
}
