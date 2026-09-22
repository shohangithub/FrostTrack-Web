using Domain;
using Domain.Entitites;

namespace Application.Services.Common;

public static class BookingDueCalculator
{
    /// <summary>
    /// Computes comprehensive accrued financial amounts for a booking taking into account:
    /// 1. Delivered goods rent, labour, and delivery adjustments.
    /// 2. Undelivered remaining stock still stored in the cold storage warehouse (initial billing cycle + any completed elapsed cycles).
    /// 3. Booking intake labour.
    /// </summary>
    public static (decimal TotalRent, decimal TotalLabour, decimal TotalAccrued, decimal PendingRecurringCharge) CalculateBookingAccruedDetails(
        Booking booking,
        IEnumerable<BookingDetail> activeDetails,
        IEnumerable<Delivery> deliveries,
        DateTime asOfDate)
    {
        decimal totalDeliveredRent = 0m;
        decimal totalDeliveredLabour = 0m;
        decimal totalDeliveryAdjustments = 0m;
        decimal totalRemainingRent = 0m;
        decimal totalBookingLabour = 0m;
        decimal pendingRecurringCharge = 0m;

        var deliveryList = deliveries?.Where(d => !d.IsDeleted).ToList() ?? new List<Delivery>();
        var detailList = activeDetails?.Where(d => !d.IsDeleted).ToList() ?? new List<BookingDetail>();

        foreach (var d in deliveryList)
        {
            totalDeliveryAdjustments += d.AdjustmentValue;
            if (d.DeliveryDetails != null && d.DeliveryDetails.Any())
            {
                totalDeliveredRent += d.DeliveryDetails.Sum(dd => dd.ChargeAmount);
                totalDeliveredLabour += d.DeliveryDetails.Sum(dd => dd.LabourCharge);
            }
            else
            {
                totalDeliveredRent += d.ChargeAmount;
            }
        }

        foreach (var bd in detailList)
        {
            totalBookingLabour += bd.LabourCharge;

            var deliveredBaseQty = deliveryList
                .SelectMany(d => d.DeliveryDetails ?? Enumerable.Empty<DeliveryDetail>())
                .Where(dd => dd.BookingDetailId == bd.Id)
                .Sum(dd => dd.BaseQuantity);

            decimal remainingRatio = bd.BaseQuantity > 0
                ? Math.Max(0m, (bd.BaseQuantity - deliveredBaseQty) / bd.BaseQuantity)
                : 0m;

            decimal remainingQty = (decimal)bd.BookingQuantity * remainingRatio;

            if (remainingQty > 0)
            {
                int additionalCycles = RecurringChargeCalculator.CompletedCycles(bd.BillType, booking.BookingDate, asOfDate);
                if (additionalCycles > 0)
                {
                    pendingRecurringCharge += remainingQty * bd.BookingRate * additionalCycles;
                }

                // 1 initial cycle + any additional completed cycles elapsed since booking
                int totalCycles = 1 + additionalCycles;
                totalRemainingRent += remainingQty * bd.BookingRate * totalCycles;
            }
        }

        decimal totalRent = totalDeliveredRent + totalRemainingRent + totalDeliveryAdjustments;
        decimal totalLabour = totalBookingLabour + totalDeliveredLabour;
        decimal totalAccrued = totalRent + totalLabour;

        return (totalRent, totalLabour, totalAccrued, pendingRecurringCharge);
    }

    public static (decimal TotalAccrued, decimal PendingRecurringCharge) CalculateBookingAccruedAmount(
        Booking booking,
        IEnumerable<BookingDetail> activeDetails,
        IEnumerable<Delivery> deliveries,
        DateTime asOfDate)
    {
        var (_, _, totalAccrued, pending) = CalculateBookingAccruedDetails(booking, activeDetails, deliveries, asOfDate);
        return (totalAccrued, pending);
    }
}
