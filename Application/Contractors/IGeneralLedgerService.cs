using Application.ReponseDTO;

namespace Application.Contractors;

public interface IGeneralLedgerService
{
    Task<GeneralLedgerResponse> GetGeneralLedgerAsync(DateTime reportDate, CancellationToken cancellationToken = default);
    Task<GeneralLedgerRangeResponse> GetGeneralLedgerRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}
