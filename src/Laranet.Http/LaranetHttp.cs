using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Laranet.Http;

/// <summary>
/// Provides a thin convenience layer over <see cref="HttpClient"/> for typed API
/// calls.
/// </summary>
/// <remarks>
/// <para>
/// This type is not a replacement for <see cref="HttpClient"/>. It delegates
/// transport, handlers, and connection management to the native implementation.
/// </para>
/// <para>
/// Caller cancellation always surfaces as <see cref="OperationCanceledException"/>.
/// Other failures are returned as an <see cref="HttpResult"/> unless
/// <see cref="RequestOptions.ThrowOnError"/> is enabled.
/// </para>
/// </remarks>
public sealed class LaranetHttp
{
    private const int MaxErrorBodyLength = 4096;

    private readonly HttpClient _httpClient;
    private readonly ILogger<LaranetHttp> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="LaranetHttp"/> class.
    /// </summary>
    /// <param name="httpClient">The client used to send requests.</param>
    /// <param name="logger">The logger used for structured request logging.</param>
    /// <param name="options">
    /// The default client options. When <see langword="null"/>, the client uses
    /// <see cref="JsonSerializerDefaults.Web"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="httpClient"/> or <paramref name="logger"/> is
    /// <see langword="null"/>.
    /// </exception>
    public LaranetHttp(
        HttpClient httpClient,
        ILogger<LaranetHttp> logger,
        IOptions<LaranetHttpOptions>? options = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = (options?.Value ?? new LaranetHttpOptions()).JsonOptions;
    }

    /// <summary>
    /// Sends a GET request and deserializes the response body.
    /// </summary>
    /// <typeparam name="T">The type of the response body.</typeparam>
    /// <param name="url">The absolute or relative request URL.</param>
    /// <param name="options">The per-request options.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The typed result of the operation.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="url"/> is <see langword="null"/>, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The configured timeout is not positive.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> is canceled.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// The request failed and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="JsonException">
    /// The response body could not be deserialized and
    /// <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// The request timed out and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The response did not match the expected contract and
    /// <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    public Task<HttpResult<T>> GetAsync<T>(
        string url,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(
            HttpMethod.Get,
            url,
            body: null,
            hasBody: false,
            options,
            cancellationToken);
    }

    /// <summary>
    /// Sends a POST request with a JSON body and ignores the response body.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <param name="url">The absolute or relative request URL.</param>
    /// <param name="body">The request body to serialize as JSON.</param>
    /// <param name="options">The per-request options.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="body"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="url"/> is <see langword="null"/>, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The configured timeout is not positive.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> is canceled.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// The request failed and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// The request timed out and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    public Task<HttpResult> PostAsync<TRequest>(
        string url,
        TRequest body,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        return SendAsync(
            HttpMethod.Post,
            url,
            body,
            hasBody: true,
            options,
            cancellationToken);
    }

    /// <summary>
    /// Sends a POST request with a JSON body and deserializes the response body.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <typeparam name="TResponse">The type of the response body.</typeparam>
    /// <param name="url">The absolute or relative request URL.</param>
    /// <param name="body">The request body to serialize as JSON.</param>
    /// <param name="options">The per-request options.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The typed result of the operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="body"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="url"/> is <see langword="null"/>, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The configured timeout is not positive.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> is canceled.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// The request failed and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="JsonException">
    /// The response body could not be deserialized and
    /// <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// The request timed out and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The response did not match the expected contract and
    /// <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    public Task<HttpResult<TResponse>> PostAsync<TRequest, TResponse>(
        string url,
        TRequest body,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        return SendAsync<TResponse>(
            HttpMethod.Post,
            url,
            body,
            hasBody: true,
            options,
            cancellationToken);
    }

    /// <summary>
    /// Sends a PUT request with a JSON body and ignores the response body.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <param name="url">The absolute or relative request URL.</param>
    /// <param name="body">The request body to serialize as JSON.</param>
    /// <param name="options">The per-request options.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="body"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="url"/> is <see langword="null"/>, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The configured timeout is not positive.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> is canceled.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// The request failed and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// The request timed out and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    public Task<HttpResult> PutAsync<TRequest>(
        string url,
        TRequest body,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        return SendAsync(
            HttpMethod.Put,
            url,
            body,
            hasBody: true,
            options,
            cancellationToken);
    }

    /// <summary>
    /// Sends a PUT request with a JSON body and deserializes the response body.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <typeparam name="TResponse">The type of the response body.</typeparam>
    /// <param name="url">The absolute or relative request URL.</param>
    /// <param name="body">The request body to serialize as JSON.</param>
    /// <param name="options">The per-request options.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The typed result of the operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="body"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="url"/> is <see langword="null"/>, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The configured timeout is not positive.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> is canceled.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// The request failed and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="JsonException">
    /// The response body could not be deserialized and
    /// <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// The request timed out and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The response did not match the expected contract and
    /// <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    public Task<HttpResult<TResponse>> PutAsync<TRequest, TResponse>(
        string url,
        TRequest body,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        return SendAsync<TResponse>(
            HttpMethod.Put,
            url,
            body,
            hasBody: true,
            options,
            cancellationToken);
    }

    /// <summary>
    /// Sends a PATCH request with a JSON body and ignores the response body.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <param name="url">The absolute or relative request URL.</param>
    /// <param name="body">The request body to serialize as JSON.</param>
    /// <param name="options">The per-request options.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="body"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="url"/> is <see langword="null"/>, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The configured timeout is not positive.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> is canceled.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// The request failed and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// The request timed out and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    public Task<HttpResult> PatchAsync<TRequest>(
        string url,
        TRequest body,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        return SendAsync(
            HttpMethod.Patch,
            url,
            body,
            hasBody: true,
            options,
            cancellationToken);
    }

    /// <summary>
    /// Sends a PATCH request with a JSON body and deserializes the response body.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <typeparam name="TResponse">The type of the response body.</typeparam>
    /// <param name="url">The absolute or relative request URL.</param>
    /// <param name="body">The request body to serialize as JSON.</param>
    /// <param name="options">The per-request options.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The typed result of the operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="body"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="url"/> is <see langword="null"/>, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The configured timeout is not positive.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> is canceled.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// The request failed and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="JsonException">
    /// The response body could not be deserialized and
    /// <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// The request timed out and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The response did not match the expected contract and
    /// <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    public Task<HttpResult<TResponse>> PatchAsync<TRequest, TResponse>(
        string url,
        TRequest body,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        return SendAsync<TResponse>(
            HttpMethod.Patch,
            url,
            body,
            hasBody: true,
            options,
            cancellationToken);
    }

    /// <summary>
    /// Sends a DELETE request and ignores the response body.
    /// </summary>
    /// <param name="url">The absolute or relative request URL.</param>
    /// <param name="options">The per-request options.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="url"/> is <see langword="null"/>, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The configured timeout is not positive.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <see cref="RequestOptions.ContentHeaders"/> is set without a request body.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> is canceled.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// The request failed and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// The request timed out and <see cref="RequestOptions.ThrowOnError"/> is enabled.
    /// </exception>
    public Task<HttpResult> DeleteAsync(
        string url,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Delete,
            url,
            body: null,
            hasBody: false,
            options,
            cancellationToken);
    }

    private async Task<HttpResult> SendAsync(
        HttpMethod method,
        string url,
        object? body,
        bool hasBody,
        RequestOptions? options,
        CancellationToken cancellationToken)
    {
        options ??= new RequestOptions();

        var response = await SendCoreAsync<object>(
            method,
            url,
            body,
            hasBody,
            expectBody: false,
            options,
            cancellationToken).ConfigureAwait(false);

        HttpResult result = response.IsSuccess
            ? HttpResult.Success(response.StatusCode!.Value, response.Headers)
            : HttpResult.Failure(
                response.ErrorKind,
                response.StatusCode,
                response.Error!,
                response.Headers);

        ThrowIfRequested(result, response.Exception, options.ThrowOnError);

        return result;
    }

    private async Task<HttpResult<T>> SendAsync<T>(
        HttpMethod method,
        string url,
        object? body,
        bool hasBody,
        RequestOptions? options,
        CancellationToken cancellationToken)
    {
        options ??= new RequestOptions();

        var response = await SendCoreAsync<T>(
            method,
            url,
            body,
            hasBody,
            expectBody: true,
            options,
            cancellationToken).ConfigureAwait(false);

        HttpResult<T> result = response.IsSuccess
            ? HttpResult<T>.Success(
                response.Value!,
                response.StatusCode!.Value,
                response.Headers)
            : HttpResult<T>.Failure(
                response.ErrorKind,
                response.StatusCode,
                response.Error!,
                response.Headers);

        ThrowIfRequested(result, response.Exception, options.ThrowOnError);

        return result;
    }

    private async Task<RawResponse<T>> SendCoreAsync<T>(
        HttpMethod method,
        string url,
        object? body,
        bool hasBody,
        bool expectBody,
        RequestOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (options.Timeout is { } configuredTimeout
            && configuredTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                configuredTimeout,
                "The timeout must be greater than zero.");
        }

        if (!hasBody && options.ContentHeaders is { Count: > 0 })
        {
            throw new ArgumentException(
                "Content headers require a request body.",
                nameof(options));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out _)
            && _httpClient.BaseAddress is null)
        {
            throw new InvalidOperationException(
                "A relative URL requires HttpClient.BaseAddress to be configured.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var jsonOptions = options.JsonOptions ?? _jsonOptions;
        using var request = new HttpRequestMessage(method, url);

        if (hasBody)
        {
            var content = JsonContent.Create(
                body,
                body!.GetType(),
                options: jsonOptions);
            ApplyHeaders(content.Headers, options.ContentHeaders);
            request.Content = content;
        }

        ApplyHeaders(request.Headers, options.Headers);

        var stopwatch = Stopwatch.StartNew();

        using var timeoutSource = options.Timeout is { } timeout
            ? new CancellationTokenSource(timeout)
            : null;
        using var linkedSource = timeoutSource is null
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token);
        var requestToken = linkedSource?.Token ?? cancellationToken;
        HttpStatusCode? statusCode = null;
        IReadOnlyDictionary<string, IReadOnlyList<string>>? headers = null;

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                requestToken).ConfigureAwait(false);

            statusCode = response.StatusCode;
            headers = CollectHeaders(response);
            var responseHeaders = headers;

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await ReadErrorBodyAsync(response, requestToken)
                    .ConfigureAwait(false);
                var error = string.IsNullOrWhiteSpace(errorBody)
                    ? $"The server returned HTTP {(int)statusCode} ({statusCode})."
                    : errorBody;

                _logger.LogWarning(
                    "HTTP {Method} {Url} returned {StatusCode} in {ElapsedMilliseconds} ms.",
                    method.Method,
                    url,
                    (int)statusCode,
                    stopwatch.Elapsed.TotalMilliseconds);

                return RawResponse<T>.Failure(
                    statusCode,
                    HttpErrorKind.Http,
                    error,
                    responseHeaders);
            }

            if (!expectBody)
            {
                _logger.LogInformation(
                    "HTTP {Method} {Url} returned {StatusCode} in {ElapsedMilliseconds} ms.",
                    method.Method,
                    url,
                    (int)statusCode,
                    stopwatch.Elapsed.TotalMilliseconds);

                return RawResponse<T>.Success(
                    value: default,
                    statusCode.Value,
                    responseHeaders);
            }

            var bodyResult = await ReadBodyAsync<T>(
                response,
                jsonOptions,
                requestToken).ConfigureAwait(false);

            stopwatch.Stop();

            if (bodyResult.IsSuccess)
            {
                _logger.LogInformation(
                    "HTTP {Method} {Url} returned {StatusCode} in {ElapsedMilliseconds} ms.",
                    method.Method,
                    url,
                    (int)statusCode,
                    stopwatch.Elapsed.TotalMilliseconds);

                return RawResponse<T>.Success(
                    bodyResult.Value,
                    statusCode.Value,
                    responseHeaders);
            }

            _logger.LogWarning(
                bodyResult.Exception,
                "HTTP {Method} {Url} returned {StatusCode} with an unreadable body in {ElapsedMilliseconds} ms.",
                method.Method,
                url,
                (int)statusCode,
                stopwatch.Elapsed.TotalMilliseconds);

            return RawResponse<T>.Failure(
                statusCode,
                bodyResult.ErrorKind,
                bodyResult.Error!,
                responseHeaders,
                bodyResult.Exception);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "HTTP {Method} {Url} timed out after {ElapsedMilliseconds} ms.",
                method.Method,
                url,
                stopwatch.Elapsed.TotalMilliseconds);

            return RawResponse<T>.Failure(
                statusCode,
                HttpErrorKind.Timeout,
                "The request timed out.",
                headers);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                exception,
                "HTTP {Method} {Url} failed after {ElapsedMilliseconds} ms.",
                method.Method,
                url,
                stopwatch.Elapsed.TotalMilliseconds);

            return RawResponse<T>.Failure(
                statusCode,
                HttpErrorKind.Network,
                exception.Message,
                headers,
                exception);
        }
    }

    private static async Task<BodyReadResult<T>> ReadBodyAsync<T>(
        HttpResponseMessage response,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.ResetContent)
        {
            return BodyReadResult<T>.Unexpected(
                "The response has no body.");
        }

        var body = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(body))
        {
            return BodyReadResult<T>.Unexpected(
                "The response body was empty.");
        }

        try
        {
            var value = JsonSerializer.Deserialize<T>(body, jsonOptions);

            if (value is null)
            {
                return BodyReadResult<T>.Unexpected(
                    "The response body was null.");
            }

            return BodyReadResult<T>.Success(value);
        }
        catch (JsonException exception)
        {
            return BodyReadResult<T>.Deserialization(
                $"The response body could not be deserialized: {exception.Message}",
                exception);
        }
    }

    private static async Task<string> ReadErrorBodyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is 0)
        {
            return string.Empty;
        }

        await using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: false);

        var buffer = new char[MaxErrorBodyLength];
        var read = await reader
            .ReadBlockAsync(buffer.AsMemory(), cancellationToken)
            .ConfigureAwait(false);

        return new string(buffer, 0, read);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> CollectHeaders(
        HttpResponseMessage response)
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var header in response.Headers)
        {
            headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            headers[header.Key] = header.Value.ToArray();
        }

        return new ReadOnlyDictionary<string, IReadOnlyList<string>>(headers);
    }

    private static void ApplyHeaders(
        HttpHeaders headers,
        IReadOnlyDictionary<string, string>? values)
    {
        if (values is null)
        {
            return;
        }

        foreach (var (name, value) in values)
        {
            headers.Remove(name);
            headers.TryAddWithoutValidation(name, value);
        }
    }

    private static void ThrowIfRequested(
        HttpResult result,
        Exception? exception,
        bool throwOnError)
    {
        if (result.IsSuccess || !throwOnError)
        {
            return;
        }

        throw result.ErrorKind switch
        {
            HttpErrorKind.Http => new HttpRequestException(
                result.Error,
                exception,
                result.StatusCode),
            HttpErrorKind.Network => exception as HttpRequestException
                ?? new HttpRequestException(result.Error, exception),
            HttpErrorKind.Timeout => new TimeoutException(result.Error, exception),
            HttpErrorKind.Deserialization => exception as JsonException
                ?? new JsonException(result.Error, exception),
            HttpErrorKind.UnexpectedResponse => new InvalidOperationException(
                result.Error,
                exception),
            _ => new HttpRequestException(
                result.Error,
                exception,
                result.StatusCode),
        };
    }

    private readonly record struct RawResponse<T>(
        bool IsSuccess,
        HttpStatusCode? StatusCode,
        HttpErrorKind ErrorKind,
        string? Error,
        T? Value,
        IReadOnlyDictionary<string, IReadOnlyList<string>> Headers,
        Exception? Exception)
    {
        public static RawResponse<T> Success(
            T? value,
            HttpStatusCode statusCode,
            IReadOnlyDictionary<string, IReadOnlyList<string>> headers)
        {
            return new RawResponse<T>(
                true,
                statusCode,
                HttpErrorKind.None,
                null,
                value,
                headers,
                null);
        }

        public static RawResponse<T> Failure(
            HttpStatusCode? statusCode,
            HttpErrorKind errorKind,
            string error,
            IReadOnlyDictionary<string, IReadOnlyList<string>>? headers = null,
            Exception? exception = null)
        {
            return new RawResponse<T>(
                false,
                statusCode,
                errorKind,
                error,
                default,
                headers ?? HttpResult.EmptyHeaders,
                exception);
        }
    }

    private readonly record struct BodyReadResult<T>(
        bool IsSuccess,
        T? Value,
        HttpErrorKind ErrorKind,
        string? Error,
        Exception? Exception)
    {
        public static BodyReadResult<T> Success(T value)
        {
            return new BodyReadResult<T>(
                true,
                value,
                HttpErrorKind.None,
                null,
                null);
        }

        public static BodyReadResult<T> Unexpected(string error)
        {
            return new BodyReadResult<T>(
                false,
                default,
                HttpErrorKind.UnexpectedResponse,
                error,
                null);
        }

        public static BodyReadResult<T> Deserialization(
            string error,
            Exception exception)
        {
            return new BodyReadResult<T>(
                false,
                default,
                HttpErrorKind.Deserialization,
                error,
                exception);
        }
    }
}
