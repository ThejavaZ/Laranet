# Validation API (`Laranet.AspNetCore.Validation`)

`Laranet.AspNetCore.Validation` is an optional adapter between FluentValidation and
ASP.NET Core Minimal API endpoint filters. It does not define a validation engine, a
request base class, or a custom error type.

## Namespace

`Laranet.AspNetCore.Validation`

## Dependencies

| Package | References |
| --- | --- |
| `Laranet.AspNetCore.Validation` | `FluentValidation` 12.x, ASP.NET Core shared framework |

The package does not reference `Laranet.AspNetCore`, `Laranet.Data`, EF Core, or any
other Laranet package. It can be installed and removed on its own.

## `WithFluentValidation<TRequest>`

```csharp
public static RouteHandlerBuilder WithFluentValidation<TRequest>(
    this RouteHandlerBuilder builder);
```

Adds `FluentValidationEndpointFilter<TRequest>` to a Minimal API endpoint. Returns the
same `RouteHandlerBuilder`, so it composes with other endpoint conventions.

```csharp
using Laranet.AspNetCore.Validation;

app.MapPost("/products", (CreateProductRequest request) =>
    Results.Created())
    .WithFluentValidation<CreateProductRequest>();
```

- Throws `ArgumentNullException` when `builder` is `null`.
- The request type must also be registered in dependency injection as described below.

## `FluentValidationEndpointFilter<TRequest>`

```csharp
public sealed class FluentValidationEndpointFilter<TRequest> : IEndpointFilter
{
    public FluentValidationEndpointFilter(
        IEnumerable<IValidator<TRequest>> validators);

    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next);
}
```

The filter resolves every `IValidator<TRequest>` registered in dependency injection.

### Behavior

- The first argument of type `TRequest` is validated. This includes request models
  bound with `[AsParameters]`.
- When no argument of type `TRequest` is present, the filter calls the next delegate
  without validating.
- Every registered validator runs concurrently. Cancellation receives
  `HttpContext.RequestAborted`.
- The results are combined into a FluentValidation `ValidationResult`. Their errors are
  grouped by property through `ToDictionary()`.
- A valid request calls the next delegate.
- An invalid request short-circuits with `Results.ValidationProblem(...)`. The endpoint
  does not run.
- No validator registered for `TRequest`: the constructor throws
  `InvalidOperationException`. Register at least one validator before adding the
  filter.

## Validator registration

The package does not register validators. Use the native container, or
`FluentValidation.DependencyInjectionExtensions` for assembly scanning:

```csharp
builder.Services.AddScoped<
    IValidator<CreateProductRequest>,
    CreateProductRequestValidator>();
```

```csharp
using FluentValidation;

builder.Services.AddValidatorsFromAssemblyContaining<
    CreateProductRequestValidator>();
```

## Error response

The response is the native ASP.NET Core `ValidationProblemDetails` shape:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["'Name' must not be empty."],
    "Price": ["'Price' must be greater than or equal to '0'."]
  }
}
```

Status is `400` and content type is `application/problem+json`. Multiple validators
that report the same property are merged into one array.

## What the package does not include

- An MVC filter. Controllers keep `[ApiController]` and DataAnnotations validation.
- A validation engine, `FormRequest`, `Result<T>`, or custom exception hierarchy.
- An `AddLaranetValidation` registration wrapper.
- Automatic endpoint discovery or global middleware. The filter applies only to
  endpoints that call `WithFluentValidation<TRequest>()`.

## Related

- [Validation guide](../guides/validation.md)
- [ADR-0011](../architecture/decisions/0011-optional-fluentvalidation-integration.md)
- [`Laranet.AspNetCore.Validation` README](../../src/Laranet.AspNetCore.Validation/README.md)
