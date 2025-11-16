using Application.Framework;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore.Query;

namespace Application.Repositories;

public interface IRepository<TEntity, KeyType>
    where TEntity : class
{
    IQueryable<TEntity> Query();
    ValueTask<PaginationResult<TResponse>> PaginationQuery<TResponse>(PaginationQuery paginationQuery, Expression<Func<TEntity, bool>>? predicate, Expression<Func<TEntity, TResponse>> selector, CancellationToken cancellationToken);
    ValueTask<PaginationResult<TResponse>> PaginationQuery<TResponse>(IQueryable<TEntity> query, PaginationQuery paginationQuery, Expression<Func<TEntity, TResponse>> selector, CancellationToken cancellationToken = default);
    ValueTask<TEntity?> GetByIdAsync(KeyType id, CancellationToken cancellationToken);
    IEnumerable<TEntity> GetAll();
    ValueTask<bool> AddAsync(TEntity entity, CancellationToken cancellationToken);
    ValueTask<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken);
    ValueTask UpdateExecuteAsync(Expression<Func<SetPropertyCalls<ApplicationUser>, SetPropertyCalls<ApplicationUser>>> props, CancellationToken cancellationToken = default);
    IQueryable<TEntity> UpdatableQuery(Expression<Func<TEntity, bool>> expression);
    IQueryable<TEntity> DeletableQuery(Expression<Func<TEntity, bool>> expression);
    ValueTask<TEntity?> UpdatePatchAsync(int id, JsonPatchDocument<TEntity> patchDocument, CancellationToken cancellationToken = default);
    ValueTask<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken);
    void Delete(params TEntity[] entities);
}
