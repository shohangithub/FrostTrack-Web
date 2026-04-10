using Application.Contractors;
using Microsoft.EntityFrameworkCore.Storage;

namespace Persistence;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => context.Database.BeginTransactionAsync(cancellationToken);
}
