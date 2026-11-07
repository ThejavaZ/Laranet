using System.Linq.Expressions;

namespace Laranet.Data.Extensions;

/// <summary>
/// Provides conditional filtering operations for queryable collections.
/// </summary>
public static class QueryableWhereIfExtensions
{
    /// <summary>
    /// Filters a query only when the specified condition is true.
    /// </summary>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> source,
        bool condition,
        Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        return condition
            ? source.Where(predicate)
            : source;
    }
}
