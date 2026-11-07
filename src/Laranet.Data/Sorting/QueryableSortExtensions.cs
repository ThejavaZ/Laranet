
using System.Linq.Expressions;
using Laranet.Data.Sorting;

namespace Laranet.Data.Extensions;

/// <summary>
/// Provides sorting operations for queryable collections.
/// </summary>
public static class QueryableSortExtensions
{
    /// <summary>
    /// Sorts a query using the specified key selector and direction.
    /// </summary>
    public static IOrderedQueryable<T> SortBy<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> selector,
        SortDirection direction = SortDirection.Ascending)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return direction switch
        {
            SortDirection.Ascending => source.OrderBy(selector),

            SortDirection.Descending => source.OrderByDescending(selector),

            _ => throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "The specified sort direction is not supported.")
        };
    }
}
