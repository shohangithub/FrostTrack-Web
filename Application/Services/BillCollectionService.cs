using Application.Contractors;
using Application.Contractors.Authentication;
using Application.Framework;
using Application.ReponseDTO;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class BillCollectionService : IBillCollectionService
{
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<Delivery, Guid> _deliveryRepository;
    private readonly Guid _tenantId;

    public BillCollectionService(
        IRepository<Booking, Guid> bookingRepository,
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<Delivery, Guid> deliveryRepository,
        ITenantProvider tenantProvider)
    {
        _bookingRepository = bookingRepository;
        _transactionRepository = transactionRepository;
        _deliveryRepository = deliveryRepository;
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
            bd.BillType == "BOOKING"
                ? (decimal)bd.BookingQuantity * bd.BookingRate
                : bd.BaseQuantity * bd.BaseRate);

        return totalAmount;
    }

    public async Task<decimal> GetBookingPaidAmountAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var paidAmount = await _transactionRepository.Query()
            .Where(t => t.BookingId == bookingId &&
                       t.TransactionType == TransactionTypes.BILL_COLLECTION &&
                       t.TransactionFlow == "IN")
            .SumAsync(t => t.Amount, cancellationToken);

        return paidAmount;
    }
}
