using Domain;
using Domain.Entitites;

namespace Application.Services.Common;

public static class BookingDueCalculator
{
    public static (decimal TotalAccrued, decimal PendingRecurringCharge) CalculateBookingAccruedAmount(
        Booking booking,
        IEnumerable<BookingDetail> activeDetails,
        decimal totalDeliveryCharge,
        DateTime asOfDate)
    {
        if (totalDeliveryCharge > 0)
        {
            var lastDeliveryDate = activeDetails.Any()
                ? activeDetails.Max(d => (DateTime?)d.LastDeliveryDate) ?? booking.BookingDate
                : booking.BookingDate;
                
            var pendingRecurringCharge = RecurringChargeCalculator.PendingRecurringChargeAmount(activeDetails, lastDeliveryDate, asOfDate);
            return (totalDeliveryCharge + pendingRecurringCharge, pendingRecurringCharge);
        }
        else
        {
            var initialAccrued = GetInitialBookingAccruedAmount(booking);
            var recurringCharge = RecurringChargeCalculator.PendingRecurringChargeAmount(activeDetails, booking.BookingDate, asOfDate);
            var totalAccrued = initialAccrued + recurringCharge;
            return (totalAccrued, recurringCharge);
        }
    }

    private static decimal GetInitialBookingAccruedAmount(Booking booking)
    {
        return booking.BookingDetails?
            .Where(d => !d.IsDeleted)
            .Sum(d => (decimal)d.BookingQuantity * d.BookingRate + d.LabourCharge) ?? 0m;
    }
}
