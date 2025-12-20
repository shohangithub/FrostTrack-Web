namespace Application.Contractors;

public interface ITrialBalanceService
{
    Task<TrialBalanceSummaryResponse> GetTrialBalanceAsync(DateTime reportDate, CancellationToken cancellationToken);
}
