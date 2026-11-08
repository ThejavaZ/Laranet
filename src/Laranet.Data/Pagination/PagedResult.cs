namespace Laranet.Data.Pagination;

/// <summary>
/// Represents a paginated collection of items and its pagination metadata.
/// </summary>
/// <typeparam name="T">The type of the collection items.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// Gets the items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int Page { get; }

    /// <summary>
    /// Gets the effective number of items per page after applying the maximum page size.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets the total number of available pages.
    /// </summary>
    /// <remarks>
    /// The value is 0 when <see cref="TotalCount"/> is 0.
    /// </remarks>
    public int TotalPages { get; }

    /// <summary>
    /// Gets whether a previous page exists.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets whether a next page exists.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
    /// </summary>
    /// <param name="items">The items in the current page.</param>
    /// <param name="page">The current page number, starting at 1.</param>
    /// <param name="pageSize">The effective number of items per page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="page"/> or <paramref name="pageSize"/> is less than 1, or
    /// <paramref name="totalCount"/> is negative.
    /// </exception>
    public PagedResult(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);

        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;

        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
    }
}
