namespace Application.Contractors;

public interface IBalanceSheetService
{
    Task<BalanceSheetSummaryResponse> GetBalanceSheetAsync(DateTime asOfDate, int? branchId, CancellationToken cancellationToken);
}
