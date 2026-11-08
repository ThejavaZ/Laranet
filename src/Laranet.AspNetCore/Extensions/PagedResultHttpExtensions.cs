using System.Globalization;
using Laranet.Data.Pagination;
using Microsoft.AspNetCore.Http;

namespace Laranet.AspNetCore.Extensions;

/// <summary>
/// Provides ASP.NET Core HTTP integration for paginated results.
/// </summary>
public static class PagedResultHttpExtensions
{
    /// <summary>
    /// Writes pagination metadata to the specified HTTP response headers.
    /// </summary>
    /// <typeparam name="T">The type of the collection items.</typeparam>
    /// <param name="result">The paginated result.</param>
    /// <param name="response">The HTTP response to update.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> or <paramref name="response"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// The following headers are written:
    /// <list type="bullet">
    /// <item><description><c>X-Total-Count</c>: the total number of items.</description></item>
    /// <item><description><c>X-Page</c>: the current page number.</description></item>
    /// <item><description><c>X-Page-Size</c>: the effective page size.</description></item>
    /// <item><description><c>X-Total-Pages</c>: the total number of pages.</description></item>
    /// </list>
    /// </remarks>
    public static void WritePaginationHeaders<T>(
        this PagedResult<T> result,
        HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(response);

        response.Headers["X-Total-Count"] = result.TotalCount
            .ToString(CultureInfo.InvariantCulture);

        response.Headers["X-Page"] = result.Page
            .ToString(CultureInfo.InvariantCulture);

        response.Headers["X-Page-Size"] = result.PageSize
            .ToString(CultureInfo.InvariantCulture);

        response.Headers["X-Total-Pages"] = result.TotalPages
            .ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Creates an <see cref="IResult"/> that writes the paginated result as JSON and
    /// includes pagination metadata in the response headers.
    /// </summary>
    /// <typeparam name="T">The type of the collection items.</typeparam>
    /// <param name="result">The paginated result.</param>
    /// <returns>An HTTP result suitable for Minimal API endpoints.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="result"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// The response body keeps the native <see cref="PagedResult{T}"/> shape. The
    /// same pagination headers written by
    /// <see cref="WritePaginationHeaders{T}"/> are added to the response.
    /// </remarks>
    public static IResult ToHttpResult<T>(this PagedResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new PagedResultHttpResult<T>(result);
    }

    private sealed class PagedResultHttpResult<T>(
        PagedResult<T> result) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            result.WritePaginationHeaders(httpContext.Response);

            return Results.Ok(result).ExecuteAsync(httpContext);
        }
    }
}
