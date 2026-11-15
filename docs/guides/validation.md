# Validation

> **Status:** Sprint 18.3 is implemented. `Laranet.AspNetCore.Validation` ships with
> `0.3.0` as an optional, independently consumable package. Native ASP.NET Core
> validation remains the default; see the [roadmap](../roadmap/v3.md).

Laranet does not introduce a validation engine, a `FormRequest` clone, or a custom
result type. ASP.NET Core and `System.ComponentModel.DataAnnotations` already provide
the primitives. `Laranet.AspNetCore.Validation` only connects FluentValidation to
Minimal API endpoint filters when a project chooses to use it.

## Request models

Keep request models separate from EF Core entities. A request type describes what the
endpoint accepts; an entity describes persistence.

```csharp
using System.ComponentModel.DataAnnotations;

public sealed record CreateProductRequest(
    [property: Required, MaxLength(255)] string Name,
    [property: Range(0, double.MaxValue)] decimal Price);
```

Using a record with `[property: ...]` targets the generated property, so the
attributes are visible to both DataAnnotations and the ASP.NET Core metadata system.

## MVC controllers

With `[ApiController]`, ASP.NET Core validates DataAnnotations automatically and
returns a `ValidationProblemDetails` response with status `400` before the action
runs:

```csharp
[ApiController]
[Route("products")]
public sealed class ProductsController(CatalogDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        // Reaching this line means the request is valid.
        var product = new Product { Name = request.Name, Price = request.Price };

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
}
```

Do not repeat the same checks inside the action. If a rule is not expressible with an
attribute, validate it in the application layer and return a `ValidationProblem`.

## Minimal APIs

### Built-in validation in .NET 10

.NET 10 includes validation services for Minimal APIs. Register them once:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidation();
```

Annotate the request type:

```csharp
public sealed record CreateProductRequest(
    [property: Required, MaxLength(255)] string Name,
    [property: Range(0, double.MaxValue)] decimal Price);
```

Use the type as a handler parameter:

```csharp
app.MapPost("/products", async (
    CreateProductRequest request,
    CatalogDbContext db,
    CancellationToken cancellationToken) =>
{
    var product = new Product { Name = request.Name, Price = request.Price };

    db.Products.Add(product);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/products/{product.Id}", product);
});
```

Invalid requests produce a `ValidationProblemDetails` response. The validation
generator discovers types referenced by endpoint signatures; use
`[ValidatableType]` when a type is not directly referenced and still needs to be
validated at runtime. See
[Validation in Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis#validation)
for the current framework details.

### Manual validation

When the project targets an earlier framework or needs full control, validate
explicitly:

```csharp
app.MapPost("/products", async (
    CreateProductRequest request,
    CatalogDbContext db,
    CancellationToken cancellationToken) =>
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        errors[nameof(request.Name)] = ["The name is required."];
    }

    if (request.Price < 0)
    {
        errors[nameof(request.Price)] = ["The price cannot be negative."];
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var product = new Product { Name = request.Name, Price = request.Price };

    db.Products.Add(product);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/products/{product.Id}", product);
});
```

The manual path keeps the same error shape as MVC and the built-in Minimal API
validation.

## FluentValidation with Laranet

FluentValidation is a separate library. Use it only when attributes are not enough —
for example, when rules depend on multiple properties, external services, or
conditional logic.

`Laranet.AspNetCore.Validation` is a thin adapter for Minimal APIs. It is not a
validation engine, it does not define a request base class, and it does not add a
custom error type. Install it only if you already use FluentValidation:

```bash
dotnet add package Laranet.AspNetCore.Validation
```

The package references `FluentValidation`. If you want assembly scanning, add
FluentValidation's dependency injection package too:

```bash
dotnet add package FluentValidation.DependencyInjectionExtensions
```

Define a request and a validator as usual:

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

Register the validator with the native container:

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

Add the filter to the endpoint:

```csharp
using Laranet.AspNetCore.Validation;

app.MapPost("/products", (CreateProductRequest request) =>
    {
        // Reaching this line means the request is valid.
        return Results.Created();
    })
    .WithFluentValidation<CreateProductRequest>();
```

The filter validates the first argument of type `CreateProductRequest`, so it also
works with `[AsParameters]` request types. All validators registered for the type run,
and their errors are combined and grouped by property. An invalid request produces the
native `ValidationProblemDetails` response with status `400` and content type
`application/problem+json`. The endpoint does not run.

Behavior to keep in mind:

- No argument of the request type: the filter passes the request through.
- No validator registered: the filter throws `InvalidOperationException` with a clear
  message. Register at least one validator before adding the filter.
- Validation receives `HttpContext.RequestAborted`.

See the [Validation API](../api/validation.md) for the complete contract.

### MVC controllers

`Laranet.AspNetCore.Validation` targets Minimal API endpoint filters only. Controllers
keep the native `[ApiController]` behavior and DataAnnotations validation. For
FluentValidation in MVC, use FluentValidation's own MVC integration or a custom
`IAsyncActionFilter`; Laranet does not ship an MVC filter.

### Opting out

The package is optional. If you do not install it, or you install it and never call
`WithFluentValidation<T>()`, native ASP.NET Core validation behaves exactly as before.
There is no global middleware and no automatic endpoint registration.

Use one validation system per request type. Mixing attributes and FluentValidation for
the same type makes the error behavior harder to predict.

## Custom validation attributes

Prefer a custom `ValidationAttribute` when a rule is reusable and synchronous:

```csharp
[AttributeUsage(AttributeTargets.Property)]
public sealed class PositivePriceAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        return value is decimal price && price >= 0
            ? ValidationResult.Success
            : new ValidationResult("The price cannot be negative.");
    }
}
```

For rules that need dependency injection or asynchronous work, use FluentValidation or
the application layer instead.

## Error response shape

Keep one shape for validation failures:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["The name is required."],
    "Price": ["The price cannot be negative."]
  }
}
```

This is `ValidationProblemDetails`, produced by MVC, the built-in Minimal API
validation, or `Results.ValidationProblem`.

## What Laranet adds

Laranet ships one optional adapter so a project that already uses FluentValidation does
not need to write its own endpoint filter. It does not replace the native mechanisms:
DataAnnotations and `AddValidation()` remain the default, and MVC continues to use
`[ApiController]` validation.

The package intentionally does not include a custom validation engine, a `FormRequest`
type, `Result<T>`, or an `AddLaranetValidation` registration wrapper. The dependency
direction and the package boundary are recorded in
[ADR-0011](../architecture/decisions/0011-optional-fluentvalidation-integration.md).

## Related

- [Validation API](../api/validation.md)
- [Error handling](error-handling.md)
- [Dependency injection](../concepts/dependency-injection.md)
- [Version 3 roadmap](../roadmap/v3.md)
- [Model validation in ASP.NET Core](https://learn.microsoft.com/aspnet/core/mvc/models/validation)
