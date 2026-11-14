# ADR-0010: Add an Optional HTTP Convenience Layer

- **Status:** Accepted
- **Date:** 2026-11-13

## Context and Problem Statement

Applications repeatedly write the same plumbing around `HttpClient`: constructing
requests, serializing and deserializing JSON, applying headers, passing cancellation
tokens, retrying transient failures, logging outcomes, and deciding what a successful
response means.

`HttpClient` is the correct transport abstraction. It already supports message
handlers, diagnostics, timeouts, dependency injection, and native integration with the
rest of .NET. Laranet should not replace it or build a parallel HTTP pipeline.

The common case, however, is still verbose: a typed request and a typed response
against a known API. The project needs to decide whether Laranet should offer a small
convenience layer, where that layer belongs, and which guarantees it must preserve.

## Decision Drivers

- Developer experience for the common case.
- Native .NET first; `HttpClient` must remain available at any time.
- Async and cancellation in every operation.
- Clear and honest error semantics.
- Safe retry behavior; no accidental duplicate writes.
- No shared mutable state.
- A small public surface.
- An optional and independently consumable package.
- No ASP.NET Core dependency.
- Testability without a real network.
- Minimal, justified dependencies.

## Considered Options

### Option 1 — No package; document native `HttpClient`

Document request construction, JSON options, headers, cancellation, and retries, and
leave every application to write its own helper.

- Advantage: no new package, no new public contract, no maintenance cost.
- Risk: every application repeats the same error-prone plumbing with inconsistent
  result and retry behavior.

### Option 2 — Build an HTTP framework

Provide a complete pipeline with policies, handlers, authentication, serialization
plugins, and an application-level client abstraction.

- Advantage: a single, highly configurable surface.
- Risk: duplicates `HttpClient`, competes with proven libraries such as Polly, grows a
  large compatibility surface, and hides the native pipeline from users.

### Option 3 — Thin convenience layer over `HttpClient`

Provide five typed operations, a small options object, a typed result, and safe
defaults. Delegate transport, handlers, DI, and serialization to .NET.

- Advantage: small, explicit, testable, and close to the platform.
- Risk: the result and retry semantics must be documented carefully, and users may
  mistake the package for a replacement for `HttpClient`.

### Option 4 — Extension methods only

Add `GetAsync<T>`, `PostAsync<T>`, and similar methods directly on `HttpClient`.

- Advantage: no new client type.
- Risk: options and result types still need a home, extension methods are harder to
  discover, and configuration becomes a long parameter list.

## Decision Outcome

Chosen option: **Option 3 — a thin convenience layer over `HttpClient`.**

The work is scheduled as Sprint 18.1 (foundation) and Sprint 18.2 (resilience and
release) in [Version 3](../../roadmap/v3.md). Sprint 18.1 freezes the first public
contract; Sprint 18.2 adds retries, logging, integration coverage, and release
readiness.

### Package boundary

- Add a new optional package, `Laranet.Http`.
- Do not change `Laranet` core or any existing package.
- Production dependencies are limited to the BCL `System.Net.Http` and
  `System.Text.Json`, `Microsoft.Extensions.Http` for dependency injection, and
  `Microsoft.Extensions.Logging.Abstractions` for structured logging.
- The package must not depend on ASP.NET Core.
- `Laranet.Http` can be constructed with an `HttpClient`. Dependency injection is the
  recommended path, not a requirement.
- The package is independently consumable. Removing it leaves native `HttpClient`
  behavior unchanged.

### Public contract

- `LaranetHttp` is a sealed class with:
  - `GetAsync<T>`
  - `PostAsync<TRequest>` and `PostAsync<TRequest, TResponse>`
  - `PutAsync<TRequest>` and `PutAsync<TRequest, TResponse>`
  - `PatchAsync<TRequest>` and `PatchAsync<TRequest, TResponse>`
  - `DeleteAsync`
- `HttpResult` represents responses without a typed body. `HttpResult<T>` represents
  responses with a typed body.
- `RequestOptions`, `RetryOptions`, and `LaranetHttpOptions` group configuration
  without turning the API into a property bag.
- `AddLaranetHttp` registers the class as a typed client through
  `AddHttpClient<LaranetHttp>` and returns the resulting `IHttpClientBuilder` for
  advanced handler configuration.
- The first version does not expose an `ILaranetHttp` interface. A sealed class is
  testable through a custom `HttpMessageHandler`; an interface is added only when a
  second implementation or a real substitution need exists.
- The package does not introduce a `Unit` type. Operations without a body return the
  non-generic `HttpResult`.

### Result and error semantics

- `HttpResult` and `HttpResult<T>` expose success, status code, error, and response
  headers. `HttpResult<T>` also exposes the typed value.
- `HttpErrorKind` distinguishes `Http`, `Network`, `Timeout`, `Deserialization`, and
  `UnexpectedResponse`.
- A transport failure has no HTTP status code, so `StatusCode` is nullable.
- Caller cancellation always throws `OperationCanceledException`; it is never converted
  into a failed result.
- `ThrowOnError` is opt-in. When enabled, failures throw native .NET exceptions such as
  `HttpRequestException` or `JsonException` instead of a Laranet exception hierarchy.
- A successful response with an empty body and a typed result is a contract error, not
  a silent `default`. Callers use the non-generic result for responses that
  intentionally have no body.
- Error bodies are bounded before they are stored in the result.
- `IsSuccess` reflects a successful HTTP response and the absence of an error. A
  response that cannot be interpreted is a failure even when the status code is 2xx.

### Retry semantics

- Retries are configured through `RetryOptions`.
- `MaxRetries` means additional attempts after the first. `MaxRetries = 2` allows at
  most three attempts.
- Only idempotent methods are retried by default: `GET`, `PUT`, and `DELETE`, plus
  `HEAD` and `OPTIONS` if they are added later.
- `POST` and `PATCH` are retried only through an explicit opt-in. The documentation
  must explain idempotency keys and the duplicate-write risk.
- Transient status codes are `408`, `429`, `500`, `502`, `503`, and `504`. Transient
  network failures are retried only for requests that are safe to retry.
- `Retry-After` is honored. Exponential backoff includes jitter and a maximum delay.
- Every attempt builds a new `HttpRequestMessage` with fresh content. A request message
  is never sent twice.
- Cancellation stops retries immediately.
- Retries never hide the final failure. The result or the thrown exception reflects the
  last attempt, and the attempt count is logged.

### Request, serialization, and logging

- Serialization uses `System.Text.Json` with web defaults. Callers can supply their own
  `JsonSerializerOptions`.
- Request headers and content headers are handled separately.
- Logging is structured and includes method, URL, status, duration, and attempt.
  Bodies and sensitive headers are not logged by default.
- URLs are logged without query-string secrets where feasible. Documentation warns
  against putting secrets in URLs.

### Escape hatch

- `HttpClient`, `HttpRequestMessage`, custom handlers, `IHttpClientFactory`, and
  libraries such as Polly remain available.
- `AddLaranetHttp` returns the native `IHttpClientBuilder`, so applications can add
  handlers and configure the primary handler.
- Laranet does not wrap or hide the native pipeline.

### What Laranet will not provide

- An HTTP server or a replacement for ASP.NET Core.
- Authentication, OAuth, token management, or identity integration.
- Multipart, streaming, or download helpers in the first version.
- A custom retry engine or a replacement for Polly.
- A custom exception hierarchy.
- Caching, proxies, telemetry, or source generation.
- A `Unit` type or a mandatory `ILaranetHttp` abstraction.

## Consequences

### Positive Consequences

- Common API calls become a single typed expression.
- Error handling, cancellation, and retries have one documented behavior.
- The package stays small and testable.
- Native `HttpClient` and its ecosystem remain available.
- Applications can adopt or remove the package without affecting other modules.

### Negative Consequences

- Another package must be versioned, tested, and documented.
- `Microsoft.Extensions.Http` and logging abstractions become dependencies of the
  package.
- Retry semantics must be explained; a convenience layer can still be misused.
- Users may expect a full resilience framework and must be directed to native
  `HttpClient` or Polly when they need one.
- The result contract becomes part of the public API and must remain stable.

## Confirmation

This decision will be validated by:

- An accepted ADR and a reviewed public API.
- Unit tests with a fake `HttpMessageHandler` covering success, error, network,
  timeout, cancellation, JSON, empty bodies, headers, and `ThrowOnError`.
- Retry tests that prove a new request is built per attempt, idempotent methods retry,
  non-idempotent methods do not retry by default, `Retry-After` is honored, and
  cancellation stops the loop.
- Integration tests against a local test server for real transport behavior.
- Documentation covering registration, options, result semantics, retries, and the
  native escape hatch.
- Verification that the production package does not reference ASP.NET Core.

## Out of Scope

- HTTP server capabilities.
- Authentication and token refresh.
- Multipart uploads, streaming, and file downloads.
- A replacement for Polly or a general resilience framework.
- Custom exception hierarchies.
- Source generation, telemetry, caching, and proxies.
- Retrying `POST` or `PATCH` without an explicit opt-in.

## Related Decisions

- [ADR-0002: Use Optional Framework Integrations](0002-optional-integrations.md)
- [ADR-0005: Keep the Core Package Intentionally Empty](0005-intentionally-empty-core.md)
- [ADR-0006: Position Laranet as a Modular Toolkit with Framework-like Tooling](0006-toolkit-with-framework-tooling.md)
- [ADR-0008: Keep Runtime and Tooling Packages on One Version Train by Default](0008-runtime-and-tooling-versioning.md)
- [Module admission gates](../ecosystem.md#9-module-admission-gates)
- [Version 3 roadmap](../../roadmap/v3.md)
