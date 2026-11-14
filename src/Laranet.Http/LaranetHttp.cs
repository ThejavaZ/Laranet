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
/// <para>
/// Transient failures are retried according to <see cref="RetryOptions"/>. Every
/// attempt builds a new <see cref="HttpRequestMessage"/> and content.
/// </para>
/// </remarks>
public sealed class LaranetHttp
{
    private const int MaxErrorBodyLength = 4096;

    private readonly HttpClient _httpClient;
    private readonly ILogger<LaranetHttp> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly RetryOptions _retryOptions;

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

        var clientOptions = options?.Value ?? new LaranetHttpOptions();
        _jsonOptions = clientOptions.JsonOptions;
        _retryOptions = clientOptions.Retry;
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
        var retry = options.Retry ?? _retryOptions;
        var canRetryMethod = IsRetryableMethod(
            method,
            retry.RetryNonIdempotentRequests);

        using var timeoutSource = options.Timeout is { } timeout
            ? new CancellationTokenSource(timeout)
            : null;
        using var linkedSource = timeoutSource is null
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token);
        var requestToken = linkedSource?.Token ?? cancellationToken;
        var attempt = 1;

        while (true)
        {
            var canRetry = canRetryMethod && attempt <= retry.MaxRetries;

            var result = await SendAttemptAsync<T>(
                method,
                url,
                body,
                hasBody,
                expectBody,
                options,
                jsonOptions,
                attempt,
                canRetry,
                requestToken,
                cancellationToken).ConfigureAwait(false);

            if (!result.IsTransient || !canRetry)
            {
                return result.Response;
            }

            var delay = GetRetryDelay(retry, attempt, result.RetryAfter);
            var statusCode = result.Response.StatusCode is { } status
                ? (int?)status
                : null;

            if (statusCode is not null)
            {
                _logger.LogWarning(
                    "HTTP {Method} {Url} attempt {Attempt} returned {StatusCode}; retrying in {RetryDelayMilliseconds} ms.",
                    method.Method,
                    url,
                    attempt,
                    statusCode,
                    delay.TotalMilliseconds);
            }
            else
            {
                _logger.LogWarning(
                    "HTTP {Method} {Url} attempt {Attempt} failed with a network error; retrying in {RetryDelayMilliseconds} ms.",
                    method.Method,
                    url,
                    attempt,
                    delay.TotalMilliseconds);
            }

            if (delay <= TimeSpan.Zero)
            {
                attempt++;
                continue;
            }

            try
            {
                await Task.Delay(delay, requestToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "HTTP {Method} {Url} timed out while waiting to retry after attempt {Attempt}.",
                    method.Method,
                    url,
                    attempt);

                return RawResponse<T>.Failure(
                    result.Response.StatusCode,
                    HttpErrorKind.Timeout,
                    "The request timed out.",
                    result.Response.Headers);
            }

            attempt++;
        }
    }

    private async Task<AttemptResult<T>> SendAttemptAsync<T>(
        HttpMethod method,
        string url,
        object? body,
        bool hasBody,
        bool expectBody,
        RequestOptions options,
        JsonSerializerOptions jsonOptions,
        int attempt,
        bool canRetry,
        CancellationToken requestToken,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        HttpStatusCode? statusCode = null;
        IReadOnlyDictionary<string, IReadOnlyList<string>>? headers = null;

        using var request = CreateRequest(
            method,
            url,
            body,
            hasBody,
            options,
            jsonOptions);

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
                var isTransient = IsTransientStatusCode(response.StatusCode);
                var retryAfter = isTransient ? GetRetryAfter(response) : null;
                var errorBody = await ReadErrorBodyAsync(response, requestToken)
                    .ConfigureAwait(false);
                var error = string.IsNullOrWhiteSpace(errorBody)
                    ? $"The server returned HTTP {(int)statusCode} ({statusCode})."
                    : errorBody;

                stopwatch.Stop();

                if (!isTransient || !canRetry)
                {
                    _logger.LogWarning(
                        "HTTP {Method} {Url} returned {StatusCode} in {ElapsedMilliseconds} ms on attempt {Attempt}.",
                        method.Method,
                        url,
                        (int)statusCode,
                        stopwatch.Elapsed.TotalMilliseconds,
                        attempt);
                }

                var failure = RawResponse<T>.Failure(
                    statusCode,
                    HttpErrorKind.Http,
                    error,
                    responseHeaders);

                return isTransient
                    ? AttemptResult<T>.Transient(failure, retryAfter)
                    : AttemptResult<T>.Final(failure);
            }

            if (!expectBody)
            {
                stopwatch.Stop();

                _logger.LogInformation(
                    "HTTP {Method} {Url} returned {StatusCode} in {ElapsedMilliseconds} ms on attempt {Attempt}.",
                    method.Method,
                    url,
                    (int)statusCode,
                    stopwatch.Elapsed.TotalMilliseconds,
                    attempt);

                return AttemptResult<T>.Final(
                    RawResponse<T>.Success(
                        value: default,
                        statusCode.Value,
                        responseHeaders));
            }

            var bodyResult = await ReadBodyAsync<T>(
                response,
                jsonOptions,
                requestToken).ConfigureAwait(false);

            stopwatch.Stop();

            if (bodyResult.IsSuccess)
            {
                _logger.LogInformation(
                    "HTTP {Method} {Url} returned {StatusCode} in {ElapsedMilliseconds} ms on attempt {Attempt}.",
                    method.Method,
                    url,
                    (int)statusCode,
                    stopwatch.Elapsed.TotalMilliseconds,
                    attempt);

                return AttemptResult<T>.Final(
                    RawResponse<T>.Success(
                        bodyResult.Value,
                        statusCode.Value,
                        responseHeaders));
            }

            _logger.LogWarning(
                bodyResult.Exception,
                "HTTP {Method} {Url} returned {StatusCode} with an unreadable body in {ElapsedMilliseconds} ms on attempt {Attempt}.",
                method.Method,
                url,
                (int)statusCode,
                stopwatch.Elapsed.TotalMilliseconds,
                attempt);

            return AttemptResult<T>.Final(
                RawResponse<T>.Failure(
                    statusCode,
                    bodyResult.ErrorKind,
                    bodyResult.Error!,
                    responseHeaders,
                    bodyResult.Exception));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "HTTP {Method} {Url} timed out after {ElapsedMilliseconds} ms on attempt {Attempt}.",
                method.Method,
                url,
                stopwatch.Elapsed.TotalMilliseconds,
                attempt);

            return AttemptResult<T>.Final(
                RawResponse<T>.Failure(
                    statusCode,
                    HttpErrorKind.Timeout,
                    "The request timed out.",
                    headers));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            stopwatch.Stop();

            if (!canRetry)
            {
                _logger.LogWarning(
                    exception,
                    "HTTP {Method} {Url} failed after {ElapsedMilliseconds} ms on attempt {Attempt}.",
                    method.Method,
                    url,
                    stopwatch.Elapsed.TotalMilliseconds,
                    attempt);
            }

            return AttemptResult<T>.Transient(
                RawResponse<T>.Failure(
                    statusCode,
                    HttpErrorKind.Network,
                    exception.Message,
                    headers,
                    exception),
                retryAfter: null);
        }
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string url,
        object? body,
        bool hasBody,
        RequestOptions options,
        JsonSerializerOptions jsonOptions)
    {
        var request = new HttpRequestMessage(method, url);

        try
        {
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

            return request;
        }
        catch
        {
            request.Dispose();
            throw;
        }
    }

    private static bool IsRetryableMethod(
        HttpMethod method,
        bool retryNonIdempotentRequests)
    {
        if (method == HttpMethod.Get
            || method == HttpMethod.Put
            || method == HttpMethod.Delete)
        {
            return true;
        }

        return retryNonIdempotentRequests
            && (method == HttpMethod.Post || method == HttpMethod.Patch);
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static TimeSpan GetRetryDelay(
        RetryOptions retry,
        int attempt,
        TimeSpan? retryAfter)
    {
        if (retryAfter is { } serverDelay)
        {
            return serverDelay < retry.MaxDelay ? serverDelay : retry.MaxDelay;
        }

        var exponential = retry.BaseDelay.TotalMilliseconds
            * Math.Pow(2, attempt - 1);
        var capped = Math.Min(exponential, retry.MaxDelay.TotalMilliseconds);
        var jitter = 0.8 + (Random.Shared.NextDouble() * 0.4);

        return TimeSpan.FromMilliseconds(
            Math.Min(capped * jitter, retry.MaxDelay.TotalMilliseconds));
    }

    private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter is null)
        {
            return null;
        }

        if (retryAfter.Delta is { } delta)
        {
            return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
        }

        if (retryAfter.Date is { } date)
        {
            var delay = date - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        return null;
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

    private readonly record struct AttemptResult<T>(
        RawResponse<T> Response,
        bool IsTransient,
        TimeSpan? RetryAfter)
    {
        public static AttemptResult<T> Final(RawResponse<T> response)
        {
            return new AttemptResult<T>(response, false, null);
        }

        public static AttemptResult<T> Transient(
            RawResponse<T> response,
            TimeSpan? retryAfter)
        {
            return new AttemptResult<T>(response, true, retryAfter);
        }
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
