# Laranet.Http

Optional convenience layer over `HttpClient` for typed, cancellation-aware API
calls.

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
    },
    cancellationToken);
```

`Headers` applies to the request. `ContentHeaders` applies to the request body and
is rejected on bodyless operations.

## Logging

Requests are logged with the HTTP method, URL, status code, and duration. Bodies
and sensitive headers are not logged. Avoid putting secrets in query strings,
because the URL is part of the log entry.

## Limitations

The first version does not include automatic retries, multipart or streaming,
authentication, or OAuth. Retries are planned as a separate step with safe defaults
for idempotent methods.

When you need full control, use `HttpClient`, `HttpRequestMessage`, custom handlers,
or a resilience library such as Polly directly. Laranet does not hide them.
