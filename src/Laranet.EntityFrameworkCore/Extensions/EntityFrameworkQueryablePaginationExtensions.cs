using Laranet.Data.Extensions;
using Laranet.Data.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Laranet.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides asynchronous Entity Framework Core pagination operations for queryable
/// collections.
/// </summary>
public static class EntityFrameworkQueryablePaginationExtensions
{
    /// <summary>
    /// Asynchronously paginates a queryable collection.
    /// </summary>
    /// <typeparam name="T">The type of the collection items.</typeparam>
    /// <param name="source">The queryable collection.</param>
    /// <param name="page">The requested page number, starting at 1.</param>
    /// <param name="pageSize">The requested number of items per page.</param>
    /// <param name="maxPageSize">The maximum number of items allowed per page.</param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the
    /// requested items and pagination metadata.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="page"/>, <paramref name="pageSize"/>, or
    /// <paramref name="maxPageSize"/> is less than 1, or the requested page offset
    /// exceeds <see cref="int.MaxValue"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The operation was canceled through <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Order the query before paginating. Without a deterministic <c>ORDER BY</c>, the
    /// provider does not guarantee a stable row order, and items may be duplicated or
    /// omitted across pages. Ordering by a unique key is recommended.
    /// </para>
    /// <para>
    /// The effective page size is the smaller of <paramref name="pageSize"/> and
    /// <paramref name="maxPageSize"/>. A page beyond the available results returns an
    /// empty <see cref="PagedResult{T}.Items"/> collection while preserving the total
    /// count.
    /// </para>
    /// <para>
    /// When the query is empty or the requested page is beyond the available results,
    /// only the count query is executed.
    /// </para>
    /// <para>
    /// This method executes queries through Entity Framework Core. Asynchronous
    /// execution is provided by the integration package so that
    /// <c>Laranet.Data</c> remains provider-neutral.
    /// </para>
    /// </remarks>
    public static async Task<PagedResult<T>> PaginateAsync<T>(
        this IQueryable<T> source,
        int page = 1,
        int pageSize = QueryablePaginationExtensions.DefaultPageSize,
        int maxPageSize = QueryablePaginationExtensions.DefaultMaxPageSize,
        CancellationToken cancellationToken = default)
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

        var totalCount = await source
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        if (totalCount == 0 || skip >= totalCount)
        {
            return new PagedResult<T>(
                Array.Empty<T>(),
                page,
                effectivePageSize,
                totalCount
            );
        }

        var items = await source
            .Skip((int)skip)
            .Take(effectivePageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<T>(
            items,
            page,
            effectivePageSize,
            totalCount
        );
    }
}
