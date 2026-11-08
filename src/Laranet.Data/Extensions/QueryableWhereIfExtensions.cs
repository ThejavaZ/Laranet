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
    /// <typeparam name="T">The type of the collection items.</typeparam>
    /// <param name="source">The queryable collection.</param>
    /// <param name="condition">The condition that determines whether the filter is applied.</param>
    /// <param name="predicate">
    /// The filter to apply when <paramref name="condition"/> is <see langword="true"/>.
    /// The argument is required even when <paramref name="condition"/> is
    /// <see langword="false"/>.
    /// </param>
    /// <returns>
    /// The filtered query when <paramref name="condition"/> is <see langword="true"/>;
    /// otherwise, the original query.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
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
