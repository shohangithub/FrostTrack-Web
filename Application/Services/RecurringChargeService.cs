namespace Application.Services;

/// <summary>
/// Daily job entry point: brings the charge ledger up to date for every active booking
/// (posts months that have started, labour, adjustments and corrections) and advances
/// BookingDetail.LastRecurringChargeDate. Safe to run any number of times.
/// </summary>
public class RecurringChargeService(IBookingLedgerService ledgerService) : IRecurringChargeService
{
    public async Task<int> ProcessRecurringChargesAsync(DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var result = await ledgerService.SyncAllAsync(asOfDate, cancellationToken: cancellationToken);
        return result.EntriesPosted;
    }
}
