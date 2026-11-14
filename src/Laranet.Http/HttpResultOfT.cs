using System.Net;

namespace Laranet.Http;

/// <summary>
/// Represents the outcome of an HTTP operation that returns a typed body.
/// </summary>
/// <typeparam name="T">The type of the response body.</typeparam>
public sealed class HttpResult<T> : HttpResult
{
    internal HttpResult(
        T? value,
        bool isSuccess,
        HttpStatusCode? statusCode,
        HttpErrorKind errorKind,
        string? error,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers)
        : base(isSuccess, statusCode, errorKind, error, headers)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the deserialized response body, or <see langword="default"/> when the
    /// operation failed.
    /// </summary>
    public T? Value { get; }

    internal static HttpResult<T> Success(
        T value,
        HttpStatusCode statusCode,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers)
    {
        return new HttpResult<T>(
            value,
            isSuccess: true,
            statusCode,
            HttpErrorKind.None,
            error: null,
            headers);
    }

    internal static new HttpResult<T> Failure(
        HttpErrorKind errorKind,
        HttpStatusCode? statusCode,
        string error,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? headers = null)
    {
        return new HttpResult<T>(
            value: default,
            isSuccess: false,
            statusCode,
            errorKind,
            error,
            headers ?? EmptyHeaders);
    }
}
