using Application.ReponseDTO;

namespace Application.Contractors;

public interface IBankBookService
{
    Task<BankBookResponse> GetBankBookAsync(DateTime reportDate, int? bankId = null, CancellationToken cancellationToken = default);
    Task<BankBookRangeResponse> GetBankBookRangeAsync(DateTime fromDate, DateTime toDate, int? bankId = null, CancellationToken cancellationToken = default);
}
