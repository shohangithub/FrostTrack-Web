namespace Application.Framework;

public static class LinqExtensions
{
    #region Query Filter Extension
    public static Expression<Func<T, bool>> AppendExpression<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right, ESearchOperator searchOperator)
    {
        switch (searchOperator)
        {
            case ESearchOperator.OR:
                if (left == null)
                {
                    left = (T model) => false;
                }

                return OrElse(left, right);

            case ESearchOperator.AND:
                if (left == null)
                {
                    left = (T model) => true;
                }

                return AndAlso(left, right);

            default:
                throw new InvalidOperationException();
        }
    }

    public static Expression<Func<T, bool>> AndAlso<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, new ExpressionParameterReplacer(right.Parameters, left.Parameters).Visit(right.Body)), left.Parameters);
    }

    public static Expression<Func<T, bool>> OrElse<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Body, new ExpressionParameterReplacer(right.Parameters, left.Parameters).Visit(right.Body)), left.Parameters);
    }

    #endregion

    #region Query Sorting Expression Builder
    public static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> source, string orderByProperty,
                 bool isAscending = true)
    {
        string command = isAscending ? "OrderBy" : "OrderByDescending";
        var type = typeof(TEntity);

        var parameterArg = Expression.Parameter(source.ElementType, "x");
        var member = orderByProperty.Split('.')
            .Aggregate((Expression)parameterArg, Expression.PropertyOrField);
        var selector = Expression.Lambda(member, parameterArg);
        var orderByCall = Expression.Call(typeof(Queryable), command,
            new Type[] { parameterArg.Type, member.Type },
            source.Expression, Expression.Quote(selector));
        return source.Provider.CreateQuery<TEntity>(orderByCall);
    }
    #endregion
}
