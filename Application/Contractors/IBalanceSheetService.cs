namespace Application.Contractors;

public interface IBalanceSheetService
{
    Task<BalanceSheetSummaryResponse> GetBalanceSheetAsync(DateTime reportDate, CancellationToken cancellationToken);
    Task<BalanceSheetComparativeResponse> GetComparativeBalanceSheetAsync(DateTime date1, DateTime date2, CancellationToken cancellationToken);
}
