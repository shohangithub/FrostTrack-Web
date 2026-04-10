using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Contractors;

public interface IUnitOfWork
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
