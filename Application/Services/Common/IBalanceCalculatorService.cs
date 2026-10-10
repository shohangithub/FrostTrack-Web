namespace Application.Services.Common;

public interface IBalanceCalculatorService
{
    /// <summary>
    /// Calculates the opening balance (Cash + Bank) up to the specified date.
    /// </summary>
    Task<decimal> GetOpeningBalanceAsync(DateTime fromUtc, DateTime toDate, bool includeBank, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the opening balance specifically for cash only.
    /// </summary>
    Task<decimal> GetCashOpeningBalanceAsync(DateTime fromUtc, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the opening balance specifically for bank only.
    /// </summary>
    Task<decimal> GetBankOpeningBalanceAsync(DateTime fromUtc, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the local calendar days within [fromUtc, toUtc) on which an explicit
    /// opening-balance entry exists (e.g. season carry-forward). On such days the cash
    /// balance is re-anchored, so range reports must re-read the opening balance.
    /// </summary>
    Task<IReadOnlyCollection<DateTime>> GetOpeningBalanceResetDaysAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}
