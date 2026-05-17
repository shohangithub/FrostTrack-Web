namespace Application.Contractors;

/// <summary>
/// Processes periodic billing recurring charges for all active bookings.
/// Called daily by the <c>BillingRecurringChargeJob</c> hosted service.
/// </summary>
public interface IRecurringChargeService
{
    /// <summary>
    /// Scans every active <see cref="Domain.Entitites.BookingDetail"/> and, for each one
    /// whose next billing cycle boundary has passed since <see cref="Domain.Entitites.BookingDetail.LastRecurringChargeDate"/>
    /// (or <see cref="Domain.Entitites.Booking.BookingDate"/> if never charged), updates
    /// <see cref="Domain.Entitites.BookingDetail.LastRecurringChargeDate"/> to the latest completed cycle boundary.
    ///
    /// This does NOT post transactions – due amounts are computed dynamically on every
    /// due-report request using <see cref="Domain.RecurringChargeCalculator"/>.
    /// </summary>
    /// <param name="asOfDate">The reference date for cycle calculations (normally UTC now).</param>
    /// <returns>The number of booking details whose recurring-charge date was advanced.</returns>
    Task<int> ProcessRecurringChargesAsync(DateTime asOfDate, CancellationToken cancellationToken = default);
}
