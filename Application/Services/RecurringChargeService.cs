namespace Application.Services;

public class RecurringChargeService(
    IRepository<Booking, Guid> bookingRepository,
    IRepository<BookingDetail, Guid> bookingDetailRepository) : IRecurringChargeService
{
    public async Task<int> ProcessRecurringChargesAsync(DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        // Load all active bookings with their details (not deleted/archived)
        var bookings = await bookingRepository.UnfilteredQuery()
            .Include(b => b.BookingDetails)
            .Where(b => !b.IsDeleted && !b.IsArchived)
            .ToListAsync(cancellationToken);

        int updated = 0;

        foreach (var booking in bookings)
        {
            foreach (var detail in booking.BookingDetails.Where(d => !d.IsDeleted))
            {
                // Cycles are always counted from the booking date (same rule as the dues)
                int charged = RecurringChargeCalculator.ChargedCycles(detail.BillType, booking.BookingDate, detail.LastRecurringChargeDate);
                int completed = RecurringChargeCalculator.CompletedCycles(detail.BillType, booking.BookingDate, asOfDate);

                if (completed <= charged)
                    continue;

                // Advance LastRecurringChargeDate to the start of the latest started cycle
                detail.LastRecurringChargeDate = RecurringChargeCalculator.CycleStart(detail.BillType, booking.BookingDate, completed);
                await bookingDetailRepository.UpdateAsync(detail, cancellationToken);
                updated++;
            }
        }

        return updated;
    }
}
