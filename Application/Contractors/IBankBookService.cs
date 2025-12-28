using Application.ReponseDTO;

namespace Application.Contractors;

public interface IBankBookService
{
    Task<BankBookResponse> GetBankBookAsync(DateTime reportDate, CancellationToken cancellationToken = default);
}
