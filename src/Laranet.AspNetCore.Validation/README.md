# Laranet.AspNetCore.Validation

Optional FluentValidation integration for ASP.NET Core Minimal APIs.

This package is a thin adapter. It does not define a validation engine, a request
base class, or a custom error type. Validators remain ordinary FluentValidation
`AbstractValidator<T>` classes, validation failures use the native
`ValidationProblemDetails` shape, and the package is only active on endpoints where
you add the filter.

## Installation

```bash
dotnet add package Laranet.AspNetCore.Validation
```

The package references `FluentValidation`. If you want assembly scanning, add
FluentValidation's dependency injection package as well:

```bash
dotnet add package FluentValidation.DependencyInjectionExtensions
```

## When to use it

Use the native ASP.NET Core options first:

- `[ApiController]` with DataAnnotations for MVC controllers.
- `AddValidation()` with DataAnnotations for Minimal APIs on .NET 10.
- Manual checks in the endpoint or application layer for small, local rules.

Add this package when you already use FluentValidation for rules that are hard to
express with attributes: cross-property rules, conditional rules, asynchronous
checks, or reusable validators.

Do not use DataAnnotations and FluentValidation for the same request type. Pick one
validation system per request.

## Usage

Define a request and a validator:

```csharp
using FluentValidation;

public sealed record CreateProductRequest(string Name, decimal Price);

public sealed class CreateProductRequestValidator
    : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(request => request.Price)
            .GreaterThanOrEqualTo(0);
    }
}
```

Register the validator with the native dependency injection container:

```csharp
builder.Services.AddScoped<
    IValidator<CreateProductRequest>,
    CreateProductRequestValidator>();
```

Or scan an assembly with FluentValidation's dependency injection package:

```csharp
using FluentValidation;

builder.Services.AddValidatorsFromAssemblyContaining<
    CreateProductRequestValidator>();
```

Add the filter to a Minimal API endpoint:

```csharp
using Laranet.AspNetCore.Validation;

app.MapPost("/products", (CreateProductRequest request) =>
    {
        // Reaching this line means the request is valid.
        return Results.Created();
    })
    .WithFluentValidation<CreateProductRequest>();
```

The filter validates the first argument of type `CreateProductRequest`. This also
works with `[AsParameters]` request types.

## Error response

An invalid request produces the same shape as native ASP.NET Core validation:

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

The response has status `400` and content type `application/problem+json`.

## Behavior

- Valid request: the endpoint runs normally.
- Invalid request: the endpoint does not run and a `ValidationProblemDetails`
  response is returned.
- Multiple validators registered for the same request type: all run and their errors
  are combined and grouped by property.
- No argument of the request type: the filter passes the request through.
- No validator registered: the filter throws `InvalidOperationException` with a clear
  message. Register at least one validator before adding the filter.
- Cancellation: validation receives `HttpContext.RequestAborted`.

## MVC controllers

MVC is not included in this package. Controllers keep the native `[ApiController]`
behavior and DataAnnotations validation. For FluentValidation in MVC, use
FluentValidation's own MVC integration or a custom `IAsyncActionFilter`; Laranet does
not ship an MVC filter.

## Opting out

The package is optional. If you do not install it, or you install it and do not call
`WithFluentValidation<T>()`, native ASP.NET Core validation behaves exactly as before.
There is no global middleware and no automatic endpoint registration.

## Requirements

- .NET 10
- ASP.NET Core 10.x
- FluentValidation 12.x

## License

Laranet is licensed under the [MIT License](https://github.com/TheJavaZ/Laranet/blob/main/LICENSE).
