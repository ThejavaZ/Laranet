# Laranet.Http

Optional convenience layer over `HttpClient` for typed, cancellation-aware API
calls, with safe retries for transient failures.

`Laranet.Http` is not a replacement for `HttpClient` and it is not an HTTP
framework. It delegates transport, handlers, connection management, and
serialization to .NET, and it keeps the native escape hatch available at any time.

## Installation

```bash
dotnet add package Laranet.Http
```

## Usage

```csharp
var user = await http.GetAsync<User>("/users/1", cancellationToken: ct);

var created = await http.PostAsync<CreateUser, User>(
    "/users",
    request,
    cancellationToken: ct);

await http.DeleteAsync("/users/1", cancellationToken: ct);
```

Register the client with dependency injection:

```csharp
builder.Services.AddLaranetHttp(options =>
{
    options.BaseAddress = new Uri("https://api.example.com");
    options.Timeout = TimeSpan.FromSeconds(30);
    options.JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    options.Retry.MaxRetries = 2;
});
```

`AddLaranetHttp` returns the native `IHttpClientBuilder`, so handlers and the
primary handler can be configured with standard .NET APIs.

## Operations

| Method | Returns |
| --- | --- |
| `GetAsync<T>` | `HttpResult<T>` |
| `PostAsync<TRequest>` | `HttpResult` |
| `PostAsync<TRequest, TResponse>` | `HttpResult<TResponse>` |
| `PutAsync<TRequest>` | `HttpResult` |
| `PutAsync<TRequest, TResponse>` | `HttpResult<TResponse>` |
| `PatchAsync<TRequest>` | `HttpResult` |
| `PatchAsync<TRequest, TResponse>` | `HttpResult<TResponse>` |
| `DeleteAsync` | `HttpResult` |

Every operation accepts optional `RequestOptions` and a `CancellationToken`.

## Results

`HttpResult` exposes:

- `IsSuccess`
- `StatusCode`, which is `null` when the request never reached the server
- `ErrorKind`: `Http`, `Network`, `Timeout`, `Deserialization`, or
  `UnexpectedResponse`
- `Error`, with the bounded response body for HTTP failures
- `Headers`, including response and content headers with all their values

`HttpResult<T>` adds the deserialized `Value`.

A successful HTTP status is not enough. A `2xx` response with an empty body or
invalid JSON is a failure, not a silent `default`.

Caller cancellation always throws `OperationCanceledException`. It is never
converted into a failed result.

## Errors and `ThrowOnError`

By default, failures are returned. Set `ThrowOnError` when the caller prefers
native .NET exceptions:

```csharp
var result = await http.GetAsync<User>(
    "/users/1",
    new RequestOptions { ThrowOnError = true },
    cancellationToken);
```

| Failure | Exception |
| --- | --- |
| HTTP status | `HttpRequestException` with `StatusCode` |
| Network | `HttpRequestException` |
| Timeout | `TimeoutException` |
| Deserialization | `JsonException` |
| Unexpected response | `InvalidOperationException` |

## Retries

`RetryOptions` retries transient failures by default:

- Status codes `408`, `429`, `500`, `502`, `503`, and `504`.
- Network failures, such as a refused connection.

`GET`, `PUT`, and `DELETE` are retried by default. `POST` and `PATCH` are not,
because repeating them can duplicate writes. Opt in per request only when the
operation is idempotent or the server supports an idempotency key:

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

`MaxRetries` counts additional attempts, so `MaxRetries = 2` allows at most three
attempts. The defaults are one second for the first delay, an exponential increase,
jitter, and a 30-second maximum delay. `Retry-After` is honored and capped by
`MaxDelay`.

Every attempt builds a new `HttpRequestMessage` and new content. The same request
message is never sent twice. Timeouts and deserialization failures are not retried,
and caller cancellation stops the loop immediately. The final result always reflects
the last attempt, and `ThrowOnError` still applies to that result.

`RequestOptions.Retry` overrides the client policy for one request.

## Request options

```csharp
var result = await http.GetAsync<User>(
    "/users/1",
    new RequestOptions
    {
        Headers = new Dictionary<string, string>
        {
            ["Accept"] = "application/json",
        },
        Timeout = TimeSpan.FromSeconds(10),
        JsonOptions = customJsonOptions,
        Retry = new RetryOptions { MaxRetries = 1 },
    },
    cancellationToken);
```

`Headers` applies to the request. `ContentHeaders` applies to the request body and
is rejected on bodyless operations.

## Logging

Requests are logged with the HTTP method, URL, status code, duration, and attempt.
Retries log the delay and the reason. Bodies and sensitive headers are not logged.
Avoid putting secrets in query strings, because the URL is part of the log entry.

## Limitations

The first version does not include multipart or streaming, authentication, OAuth, or
a custom retry engine. Use `HttpClient` handlers or a resilience library such as Polly
when you need circuit breakers, bulkheads, hedging, or a broader policy pipeline.

When you need full control, use `HttpClient`, `HttpRequestMessage`, custom handlers,
or a resilience library such as Polly directly. Laranet does not hide them.
