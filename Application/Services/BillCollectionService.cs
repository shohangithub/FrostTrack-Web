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

        var bookingsWithDue = new List<Lookup<Guid>>();

        foreach (var booking in bookings)
        {
            var totalAmount = await GetBookingTotalAmountAsync(booking.Id, cancellationToken);
            var paidAmount = await GetBookingPaidAmountAsync(booking.Id, cancellationToken);
            var dueAmount = totalAmount - paidAmount;

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
        var bookingDetails = await _bookingRepository.Query()
            .Where(b => b.Id == bookingId && b.TenantId == _tenantId)
            .SelectMany(b => b.BookingDetails)
            .ToListAsync(cancellationToken);

        var totalAmount = bookingDetails.Sum(bd =>
            bd.BillType == "MONTHLY"
                ? (decimal)bd.BookingQuantity * bd.BookingRate
                : bd.BaseQuantity * bd.BaseRate);

        return totalAmount;
    }

    public async Task<decimal> GetBookingPaidAmountAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var paidAmount = await _transactionRepository.Query()
            .Where(t => t.BookingId == bookingId &&
                       t.TransactionHead!.UsageFor == UsageFor.BILL_COLLECTION &&
                       t.TransactionHead!.Type == TransactionHeadTypes.CREDIT )
            .SumAsync(t => t.Amount, cancellationToken);

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
            EntityName = "BOOKING",
            EntityId = request.BookingId.ToString(),
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
}
