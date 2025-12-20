namespace Application.Contractors;

public interface IBalanceSheetService
{
    Task<BalanceSheetSummaryResponse> GetBalanceSheetAsync(DateTime reportDate, CancellationToken cancellationToken);
}
