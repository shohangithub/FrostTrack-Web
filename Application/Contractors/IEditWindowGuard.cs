namespace Application.Contractors;

/// <summary>
/// Enforces the company-level edit/delete window for bookings, deliveries and transactions.
/// The window is measured from the record's own date (BookingDate / DeliveryDate / TransactionDate).
/// </summary>
public interface IEditWindowGuard
{
    Task<EditWindowPolicyResponse> GetPolicyAsync(CancellationToken cancellationToken = default);
    Task EnsureEditableAsync(DateTime recordDate, string recordName, CancellationToken cancellationToken = default);
}
