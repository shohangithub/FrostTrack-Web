using Application.ReponseDTO;

namespace Application.Contractors
{
    public interface ILedgerBookService
    {
        Task<LedgerBookResponse> GetGeneralLedgerAsync(DateTime reportDate, CancellationToken cancellationToken = default);
    }
}
