using Domain;

namespace Persistence.Repositories;

public class Repository<TEntity, KeyType>(ApplicationDbContext _context) : IRepository<TEntity, KeyType> where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet = _context.Set<TEntity>() ?? throw new ArgumentNullException("dbset can't be null !");

    public IQueryable<TEntity> Query() => _dbSet.AsNoTracking().AsQueryable();

    public async ValueTask<TEntity?> GetByIdAsync(KeyType id, CancellationToken cancellationToken = default) => await _context.Set<TEntity>().FindAsync(id, cancellationToken);

    public async ValueTask<bool> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async ValueTask<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return entity;
    }

    public async ValueTask UpdateExecuteAsync(Expression<Func<SetPropertyCalls<ApplicationUser>, SetPropertyCalls<ApplicationUser>>> props, CancellationToken cancellationToken = default)
    {

    }

    public async ValueTask<TEntity?> UpdatePatchAsync(int id, JsonPatchDocument<TEntity> patchDocument, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Set<TEntity>().FindAsync(id, cancellationToken);

        if (entity == null)
        {
            throw new Exception();
        }
        patchDocument.ApplyTo(entity);

        _context.Entry(entity).State = EntityState.Modified;
        var result = await _context.SaveChangesAsync();

        return result > 0 ? entity : null;
    }

    public IQueryable<TEntity> UpdatableQuery(Expression<Func<TEntity, bool>> expression) => _context.Set<TEntity>().Where(expression);
    public IQueryable<TEntity> DeletableQuery(Expression<Func<TEntity, bool>> expression) => _context.Set<TEntity>().Where(expression);


    private Dictionary<string, object?> GetUpdatingProperties(dynamic dynamicObject)
    {

        return new Dictionary<string, object?>();
        //{
        //    { FieldName1, FieldValue2 },
        //    { FieldName2, FieldValue2 }
        //}
    }

    public async ValueTask<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        var result = await _context.SaveChangesAsync(cancellationToken);
        return result > 0;
    }
    public void Delete(params TEntity[] entities)
    {
        _dbSet.RemoveRange(entities);
        _context.SaveChanges();
    }

    public IEnumerable<TEntity> GetAll()
    {
        return _dbSet.ToList();
    }

    public void Add(TEntity entity)
    {
        throw new NotImplementedException();
    }

    public void Update(TEntity entity)
    {
        throw new NotImplementedException();
    }




    Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> CombineSetters(IEnumerable<Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>> setters)
    {
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> expr = sett => sett;

        foreach (var expr2 in setters)
        {
            var call = (MethodCallExpression)expr2.Body;
            expr = Expression.Lambda<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>(
                Expression.Call(expr.Body, call.Method, call.Arguments),
                expr2.Parameters
            );
        }

        return expr;
    }

    public async ValueTask<PaginationResult<TResponse>> PaginationQuery<TResponse>(PaginationQuery paginationQuery, Expression<Func<TEntity, bool>>? predicate, Expression<Func<TEntity, TResponse>> selector, CancellationToken cancellationToken = default)
    {
        Expression<Func<TEntity, bool>> expression = obj => true;

        if (predicate is not null)
        {
            expression = expression.AppendExpression(predicate, ESearchOperator.AND);
        }

        var query = _dbSet.AsNoTracking().Where(expression);

        if (paginationQuery.OrderBy is not null)
        {
            query = query.OrderBy<TEntity>(paginationQuery.OrderBy, paginationQuery.IsAscending ?? true);
        }

        var responseQuery = query.Select(selector);

        return await PaginationResult<TResponse>.CreateAsync(query: responseQuery, pageIndex: paginationQuery.PageIndex, pageSize: paginationQuery.PageSize, cancellationToken);
    }

    public async ValueTask<PaginationResult<TResponse>> PaginationQuery<TResponse>(IQueryable<TEntity> dbQuery, PaginationQuery paginationQuery, Expression<Func<TEntity, TResponse>> selector, CancellationToken cancellationToken = default)
    {


        var query = dbQuery;

        if (paginationQuery.OrderBy is not null)
        {
            query = query.OrderBy<TEntity>(paginationQuery.OrderBy, paginationQuery.IsAscending ?? true);
        }

        var responseQuery = query.Select(selector);

        return await PaginationResult<TResponse>.CreateAsync(query: responseQuery, pageIndex: paginationQuery.PageIndex, pageSize: paginationQuery.PageSize, cancellationToken);
    }

}
