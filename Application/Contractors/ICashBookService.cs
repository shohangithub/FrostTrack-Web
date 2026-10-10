using Application.ReponseDTO;

namespace Application.Contractors
{
    public interface ICashBookService
    {
        Task<CashBookResponse> GetCashBookAsync(DateTime reportDate, CancellationToken cancellationToken = default);
        Task<CashBookRangeResponse> GetCashBookRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    }
}
