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
                var fromDate = detail.LastRecurringChargeDate ?? booking.BookingDate;
                int newCycles = RecurringChargeCalculator.CompletedCycles(detail.BillType, fromDate, asOfDate);

                if (newCycles <= 0)
                    continue;

                // Advance LastRecurringChargeDate to the latest completed cycle boundary
                detail.LastRecurringChargeDate = AdvanceByPeriods(detail.BillType, fromDate, newCycles);
                await bookingDetailRepository.UpdateAsync(detail, cancellationToken);
                updated++;
            }
        }

        return updated;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static DateTime AdvanceByPeriods(string billType, DateTime from, int periods) =>
        billType.ToUpperInvariant() switch
        {
            BillTypes.Monthly => from.AddMonths(periods),
            BillTypes.Daily => from.AddDays(periods),
            BillTypes.Weekly => from.AddDays(periods * 7),
            BillTypes.Yearly => from.AddYears(periods),
            BillTypes.Hourly => from.AddHours(periods),
            _ => from.AddMonths(periods)
        };
}
