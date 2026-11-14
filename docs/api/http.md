# HTTP API (`Laranet.Http`)

`Laranet.Http` is an optional convenience layer over `HttpClient` for typed API
calls. It is not an HTTP framework and it does not replace `HttpClient`, custom
handlers, `IHttpClientFactory`, or resilience libraries.

## Namespace

`Laranet.Http`

## `LaranetHttp`

```csharp
public sealed class LaranetHttp
{
    public LaranetHttp(
        HttpClient httpClient,
        ILogger<LaranetHttp> logger,
        IOptions<LaranetHttpOptions>? options = null);

    public Task<HttpResult<T>> GetAsync<T>(
        string url,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default);

    public Task<HttpResult> PostAsync<TRequest>(
        string url,
        TRequest body,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default);

    public Task<HttpResult<TResponse>> PostAsync<TRequest, TResponse>(
        string url,
        TRequest body,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default);

    public Task<HttpResult> PutAsync<TRequest>(...);
    public Task<HttpResult<TResponse>> PutAsync<TRequest, TResponse>(...);
    public Task<HttpResult> PatchAsync<TRequest>(...);
    public Task<HttpResult<TResponse>> PatchAsync<TRequest, TResponse>(...);
    public Task<HttpResult> DeleteAsync(
        string url,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

Common calls are a single expression:

```csharp
var user = await http.GetAsync<User>("/users/1", cancellationToken: ct);

var created = await http.PostAsync<CreateUser, User>(
    "/users",
    request,
    cancellationToken: ct);

await http.DeleteAsync("/users/1", cancellationToken: ct);
```

## `HttpResult` and `HttpResult<T>`

`HttpResult` represents an operation without a typed body. `HttpResult<T>` adds the
deserialized `Value`.

| Member | Meaning |
| --- | --- |
| `IsSuccess` | The HTTP response succeeded and its body was interpreted when required. |
| `StatusCode` | The HTTP status, or `null` when the request never reached the server. |
| `ErrorKind` | `None`, `Http`, `Network`, `Timeout`, `Deserialization`, or `UnexpectedResponse`. |
| `Error` | The bounded error body for HTTP failures, or a description for other failures. |
| `Headers` | Response and content headers, preserving every value. |
| `Value` | The typed body on `HttpResult<T>`, or `default` on failure. |

A `2xx` response with an empty body, a `null` body, or invalid JSON is a failure,
not a silent default. Use the non-generic result for operations that intentionally
return no body.

Caller cancellation always throws `OperationCanceledException`. It is never
converted into a failed result.

## `RequestOptions`

| Property | Meaning |
| --- | --- |
| `Headers` | Request headers. |
| `ContentHeaders` | Content headers for the request body. Rejected on bodyless operations. |
| `Timeout` | Timeout for the whole operation, including retry delays and reading the response. |
| `JsonOptions` | JSON options for this request. Falls back to the client defaults. |
| `Retry` | Retry policy for this request. Falls back to the client defaults. |
| `ThrowOnError` | Throws native .NET exceptions instead of returning a failed result. |

## `RetryOptions`

| Property | Default | Meaning |
| --- | --- | --- |
| `MaxRetries` | `2` | Additional attempts after the first. `2` allows at most three attempts. |
| `BaseDelay` | One second | Delay used by the first retry. Later retries grow exponentially. |
| `MaxDelay` | 30 seconds | Maximum delay between attempts, including `Retry-After`. |
| `RetryNonIdempotentRequests` | `false` | Allows retries for `POST` and `PATCH`. |

Transient status codes are `408`, `429`, `500`, `502`, `503`, and `504`. Transient
network failures are retried too. `GET`, `PUT`, and `DELETE` are retried by default.
`POST` and `PATCH` require an explicit opt-in because repeating them can duplicate
writes:

```csharp
var result = await http.PostAsync(
    "/orders",
    order,
    new RequestOptions
    {
        Retry = new RetryOptions
        {
            MaxRetries = 2,
            RetryNonIdempotentRequests = true,
        },
    },
    cancellationToken);
```

Delays use exponential backoff with jitter. `Retry-After` from the server is honored
and capped by `MaxDelay`. Every attempt builds a new `HttpRequestMessage` and new
content; the same message is never sent twice. Timeouts and response contract errors
are not retried. Caller cancellation stops the loop immediately and throws
`OperationCanceledException`. The final result or exception always reflects the last
attempt.

## `ThrowOnError`

| Failure | Exception |
| --- | --- |
| HTTP status | `HttpRequestException` with `StatusCode` |
| Network | `HttpRequestException` |
| Timeout | `TimeoutException` |
| Deserialization | `JsonException` |
| Unexpected response | `InvalidOperationException` |

## `LaranetHttpOptions`

| Property | Meaning |
| --- | --- |
| `BaseAddress` | Base address for relative request URLs. Must be absolute. |
| `Timeout` | Timeout applied to each HTTP attempt. When `null`, `HttpClient.Timeout` applies. Use `RequestOptions.Timeout` for a whole-operation timeout. |
| `JsonOptions` | Default JSON options. Uses web defaults. |
| `Retry` | Default retry policy. Retries idempotent requests. |

## Dependency injection

`AddLaranetHttp` lives in the `Microsoft.Extensions.DependencyInjection` namespace.

```csharp
builder.Services.AddLaranetHttp(options =>
{
    options.BaseAddress = new Uri("https://api.example.com");
    options.Timeout = TimeSpan.FromSeconds(30);
    options.Retry.MaxRetries = 2;
});
```

It registers `LaranetHttp` as a typed client and returns the native
`IHttpClientBuilder`:

```csharp
builder.Services
    .AddLaranetHttp(options => options.BaseAddress = baseAddress)
    .AddHttpMessageHandler<CorrelationHandler>();
```

## Logging

Requests are logged with the HTTP method, URL, status code, duration, and attempt.
Retries log the delay and the reason. Bodies and sensitive headers are not logged.
Avoid putting secrets in query strings, because the URL is part of the log entry.

## Current limitations

The first version does not include multipart or streaming, authentication, OAuth, a
custom retry engine, or a custom exception hierarchy. Use native handlers or a
resilience library such as Polly when you need circuit breakers, bulkheads, hedging,
or a broader policy pipeline.

For full control, use `HttpClient`, `HttpRequestMessage`, custom handlers, or a
resilience library such as Polly directly. Laranet does not hide them.
