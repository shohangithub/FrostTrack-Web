namespace Application.Contractors;

public interface ITrialBalanceService
{
    Task<TrialBalanceSummaryResponse> GetTrialBalanceAsync(DateTime startDate, DateTime endDate, int? branchId, CancellationToken cancellationToken);
}
