
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
    /// <typeparam name="T">The type of the collection items.</typeparam>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="source">The queryable collection.</param>
    /// <param name="selector">The expression that selects the sort key.</param>
    /// <param name="direction">The sort direction. The default is
    /// <see cref="SortDirection.Ascending"/>.</param>
    /// <returns>An ordered query that can be composed with further sorting operations.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="direction"/> is not a supported <see cref="SortDirection"/> value.
    /// </exception>
    /// <remarks>
    /// The selector is translated by the underlying provider. Null ordering and
    /// comparison semantics depend on the provider and the configured database
    /// collation.
    /// </remarks>
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
