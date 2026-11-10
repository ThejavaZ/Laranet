# Error Handling

Laranet does not introduce its own error model, result wrapper, or exception
hierarchy. Applications use the native ASP.NET Core error-handling features, and
Laranet APIs throw standard .NET exceptions.

## What Laranet throws

The data APIs validate their public arguments and throw standard exceptions:

| Exception | Typical cause |
| --- | --- |
| `ArgumentNullException` | A required argument is `null`, such as `source`, a selector, or an allowlist. |
| `ArgumentException` | A property name is invalid, not in an allowlist, or a selector collection is empty. |
| `ArgumentOutOfRangeException` | A page, page size, maximum page size, or sort direction is invalid. |
| `OperationCanceledException` | An asynchronous operation is canceled through its `CancellationToken`. |

These exceptions indicate a programming or input error. Do not catch them only to
return a generic error response; validate input at the boundary first.

## ProblemDetails

ASP.NET Core provides [ProblemDetails](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling#problem-details)
as the standard error shape. Register it once:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
```

With this configuration:

- Unhandled exceptions become a `ProblemDetails` response without leaking stack
  traces in production.
- Status codes without a body, such as `404`, also receive a `ProblemDetails` body.

## Validation errors

Return `ValidationProblemDetails` for invalid input. In MVC, `[ApiController]`
produces it automatically when model validation fails. In Minimal APIs, build it
explicitly when validation is manual:

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

    // Persist the product.
    return Results.Created($"/products/{request.Name}", request);
});
```

Use the same shape for both MVC and Minimal APIs so clients can handle errors
consistently.

## Provider errors

Database errors arrive as provider exceptions, such as
`Microsoft.EntityFrameworkCore.DbUpdateException`. Handle them where the application
has enough context to decide the response. A unique-constraint violation, for example,
often maps to `409 Conflict`, while a connectivity failure is a server-side problem.

Do not translate every provider exception into the same response. The useful mapping
depends on the operation and the database.

## Endpoint-level results

For expected outcomes, return explicit results instead of throwing:

```csharp
app.MapGet("/products/{id:int}", async (
    int id,
    CatalogDbContext db,
    CancellationToken cancellationToken) =>
{
    var product = await db.Products.FindAsync([id], cancellationToken);

    return product is null
        ? Results.NotFound()
        : Results.Ok(product);
});
```

Reserve exceptions for unexpected failures and invalid program state.

## Why there is no `Result<T>`

Laranet deliberately does not ship a transversal `Result<T>` or `Error` type. The
project has not found a use case that native exceptions, `ProblemDetails`, and explicit
return values do not already cover. Adding a second error model would:

- Force every caller to unwrap a custom type.
- Duplicate behavior that ASP.NET Core already provides.
- Create a long-term compatibility obligation.

This position is recorded in the [ecosystem architecture](../architecture/ecosystem.md#11-explicit-non-goals).

## Logging

Log unexpected exceptions with `ILogger<T>` before the error handler converts them into
a response. Include the request identifier and enough context to reproduce the
problem, but never log secrets or full connection strings.

## Related

- [Validation](validation.md)
- [Data access](data-access.md)
- [ASP.NET Core error handling](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling)
