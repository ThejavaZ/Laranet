using Laranet.Data.Pagination;

namespace Laranet.Data.Extensions;

/// <summary>
/// Provides pagination operations for queryable collections.
/// </summary>
public static class QueryablePaginationExtensions
{
    /// <summary>
    /// Gets the default number of items per page.
    /// </summary>
    public const int DefaultPageSize = 15;

    /// <summary>
    /// Gets the default maximum number of items per page.
    /// </summary>
    public const int DefaultMaxPageSize = 100;

    /// <summary>
    /// Paginates a queryable collection.
    /// </summary>
    /// <typeparam name="T">The type of the collection items.</typeparam>
    /// <param name="source">The queryable collection.</param>
    /// <param name="page">The requested page number.</param>
    /// <param name="pageSize">The requested number of items per page.</param>
    /// <param name="maxPageSize">The maximum number of items allowed per page.</param>
    /// <returns>A paginated result containing the requested items and metadata.</returns>
    public static PagedResult<T> Paginate<T>(
        this IQueryable<T> source,
        int page = 1,
        int pageSize = DefaultPageSize,
        int maxPageSize = DefaultMaxPageSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPageSize, 1);

        var effectivePageSize = Math.Min(pageSize, maxPageSize);

        var skip = ((long)page - 1) * effectivePageSize;

        if (skip > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(page),
                page,
                "The requested page exceeds the supported offset range.");
        }

        var totalCount = source.Count();

        var items = source
            .Skip((int)skip)
            .Take(effectivePageSize)
            .ToList();

        return new PagedResult<T>(
            items,
            page,
            effectivePageSize,
            totalCount
        );
    }
}
