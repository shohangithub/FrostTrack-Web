namespace Application.Contractors;

/// <summary>
/// Manual recurring-charge management: preview what would change, apply it, and query history.
/// Used both by the admin UI and (read-only) by the BillingRecurringChargeJob for logging.
/// </summary>
public interface IRecurringChargeManagementService
{
    /// <summary>
    /// Calculates what recurring charges would be applied as of <paramref name="asOfDate"/>
    /// without persisting any changes. Safe to call multiple times.
    /// </summary>
    Task<RecurringChargePreviewResponse> PreviewAsync(DateTime asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies recurring charges as of the date in <paramref name="request"/> and writes an
    /// immutable <see cref="Domain.Entitites.RecurringChargeRun"/> audit record.
    /// Throws <see cref="InvalidOperationException"/> if another run is already in progress
    /// for this tenant.
    /// </summary>
    Task<RecurringChargeRunResponse> ApplyManualRecurringChargeAsync(RecurringChargeRunRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent recurring-charge run records for this tenant (newest first).</summary>
    Task<List<RecurringChargeRunResponse>> GetHistoryAsync(int take = 30, CancellationToken cancellationToken = default);
}
