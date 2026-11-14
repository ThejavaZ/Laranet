using System.Collections.ObjectModel;
using System.Net;

namespace Laranet.Http;

/// <summary>
/// Represents the outcome of an HTTP operation that does not return a typed body.
/// </summary>
public class HttpResult
{
    internal static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyHeaders =
        new ReadOnlyDictionary<string, IReadOnlyList<string>>(
            new Dictionary<string, IReadOnlyList<string>>(
                StringComparer.OrdinalIgnoreCase));

    internal HttpResult(
        bool isSuccess,
        HttpStatusCode? statusCode,
        HttpErrorKind errorKind,
        string? error,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        ErrorKind = errorKind;
        Error = error;
        Headers = headers;
    }

    /// <summary>
    /// Gets a value indicating whether the operation completed successfully.
    /// </summary>
    /// <remarks>
    /// A successful HTTP status code is not enough. When a typed body is expected and
    /// it cannot be interpreted, <see cref="IsSuccess"/> is <see langword="false"/>.
    /// </remarks>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the HTTP status code, or <see langword="null"/> when the request did not
    /// reach the server, for example because of a network failure or a timeout.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>
    /// Gets the category of the failure, or <see cref="HttpErrorKind.None"/> when the
    /// operation succeeded.
    /// </summary>
    public HttpErrorKind ErrorKind { get; }

    /// <summary>
    /// Gets the error description, or <see langword="null"/> when the operation
    /// succeeded. For HTTP failures, this is the bounded response body when available.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets the response headers. Response and content headers are both included. A
    /// header with multiple values keeps every value.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; }

    internal static HttpResult Success(
        HttpStatusCode statusCode,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers)
    {
        return new HttpResult(
            isSuccess: true,
            statusCode,
            HttpErrorKind.None,
            error: null,
            headers);
    }

    internal static HttpResult Failure(
        HttpErrorKind errorKind,
        HttpStatusCode? statusCode,
        string error,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? headers = null)
    {
        return new HttpResult(
            isSuccess: false,
            statusCode,
            errorKind,
            error,
            headers ?? EmptyHeaders);
    }
}
