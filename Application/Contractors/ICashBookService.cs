using Application.ReponseDTO;

namespace Application.Contractors
{
    public interface ICashBookService
    {
        Task<CashBookResponse> GetCashBookAsync(DateTime reportDate, CancellationToken cancellationToken = default);
    }
}
