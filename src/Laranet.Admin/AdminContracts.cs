using System.Security.Claims;

namespace Laranet.Admin;

/// <summary>
/// Represents a page of provider-neutral administrative rows.
/// </summary>
public sealed record AdminPage(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Items,
    int Page,
    int PageSize,
    long TotalCount)
{
    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// Represents list and filter input from an administrative screen.
/// </summary>
public sealed record AdminQuery(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    IReadOnlyDictionary<string, string?>? Filters = null,
    string? SortBy = null,
    bool SortDescending = false)
{
    /// <summary>
    /// Validates and normalizes the query values.
    /// </summary>
    public AdminQuery Normalize()
    {
        var page = Math.Max(Page, 1);
        var pageSize = Math.Clamp(PageSize, 1, 100);

        return this with
        {
            Page = page,
            PageSize = pageSize,
            Filters = Filters ?? new Dictionary<string, string?>(),
        };
    }
}

/// <summary>
/// Provides list and CRUD operations for an administrative resource.
/// </summary>
public interface IAdminDataAdapter
{
    /// <summary>
    /// Lists a resource using provider-specific data access.
    /// </summary>
    Task<AdminPage> ListAsync(
        AdminResource resource,
        AdminQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a resource row by its key.
    /// </summary>
    Task<IReadOnlyDictionary<string, object?>?> ReadAsync(
        AdminResource resource,
        object key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a resource row.
    /// </summary>
    Task<IReadOnlyDictionary<string, object?>> CreateAsync(
        AdminResource resource,
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a resource row.
    /// </summary>
    Task<IReadOnlyDictionary<string, object?>> UpdateAsync(
        AdminResource resource,
        object key,
        IReadOnlyDictionary<string, object?> values,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a resource row.
    /// </summary>
    Task DeleteAsync(
        AdminResource resource,
        object key,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Optionally supplies relationship lookup values and bulk actions to an admin UI.
/// </summary>
public interface IAdminEnhancementAdapter
{
    /// <summary>Gets options for a relationship selector.</summary>
    Task<IReadOnlyList<AdminLookupOption>> GetLookupAsync(
        AdminResource resource,
        AdminRelation relation,
        CancellationToken cancellationToken = default);

    /// <summary>Executes a bulk action for selected keys.</summary>
    Task<int> ExecuteBulkActionAsync(
        AdminResource resource,
        string action,
        IReadOnlyList<object> keys,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Decides whether the current principal may perform an administrative operation.
/// </summary>
public interface IAdminAuthorization
{
    /// <summary>
    /// Determines whether an operation is allowed.
    /// </summary>
    Task<bool> AuthorizeAsync(
        ClaimsPrincipal principal,
        AdminResource resource,
        AdminOperation operation,
        CancellationToken cancellationToken = default);
}
