namespace Application.Contractors;

public interface ITrialBalanceService
{
    Task<TrialBalanceSummaryResponse> GetTrialBalanceAsync(DateTime reportDate, CancellationToken cancellationToken);
    Task<TrialBalanceSummaryResponse> GetTrialBalanceRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken);
}
