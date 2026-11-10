# Validation

> **Status:** Laranet 0.1.0 does not ship a validation package. This guide documents
> the native ASP.NET Core options that work with the current packages. An optional
> integration is planned for Version 3; see the [roadmap](../roadmap/v3.md).

Laranet does not introduce a validation engine, a `FormRequest` clone, or a custom
result type. ASP.NET Core and `System.ComponentModel.DataAnnotations` already provide
the primitives, and FluentValidation can be added as an independent library when the
project needs it.

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

## FluentValidation

FluentValidation is a separate library. Install it only when attributes are not enough
— for example, when rules depend on multiple properties, external services, or
conditional logic.

```bash
dotnet add package FluentValidation
```

```csharp
using FluentValidation;

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
builder.Services.AddScoped<IValidator<CreateProductRequest>, CreateProductRequestValidator>();
```

Validate it in an endpoint filter:

```csharp
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();

        if (argument is null)
        {
            return await next(context);
        }

        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);

        if (result.IsValid)
        {
            return await next(context);
        }

        var errors = result.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors);
    }
}
```

```csharp
app.MapPost("/products", CreateProduct)
    .AddEndpointFilter<ValidationFilter<CreateProductRequest>>();
```

Use one validation system per project. Mixing attributes and FluentValidation for the
same request type makes the error behavior harder to predict.

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

## What Laranet will add

Version 3 studies an optional validation integration that wires the native mechanisms
without replacing them. It will not introduce a custom validation engine or a
`FormRequest` type. The package boundary (`Laranet.AspNetCore.Validation` or a broader
`Laranet.Validation`) and the final scope are recorded in the
[Version 3 roadmap](../roadmap/v3.md).

## Related

- [Error handling](error-handling.md)
- [Dependency injection](../concepts/dependency-injection.md)
- [Version 3 roadmap](../roadmap/v3.md)
- [Model validation in ASP.NET Core](https://learn.microsoft.com/aspnet/core/mvc/models/validation)
