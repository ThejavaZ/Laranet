# Dependency Injection

Laranet does not ship a dependency injection container, a service locator, or a
mandatory registration helper. Applications use
`Microsoft.Extensions.DependencyInjection`, the same as any other .NET application.

## Why there is no `AddLaranet()` method

The published packages expose stateless extension methods and value types. They do not
register services, so there is nothing to add to the container.

Navigating the native container directly:

- Keeps the middleware order, lifetimes, and options under the application's control.
- Avoids hiding framework configuration behind a wrapper.
- Prevents Laranet from owning the application lifecycle.

A registration helper would only be justified when a package needs to register
services, options, or a hosted component. If that happens, it will be added to the
integration that owns those services, not to the core package.

## Registering EF Core

Register the `DbContext` with the native EF Core API:

```csharp
var connectionString = builder.Configuration.GetConnectionString("Catalog");

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlite(connectionString));
```

`PaginateAsync` is an extension method on `IQueryable<T>`. It does not need to be
registered:

```csharp
app.MapGet("/products", async (
    CatalogDbContext db,
    CancellationToken cancellationToken) =>
{
    var products = await db.Products
        .OrderBy(product => product.Id)
        .PaginateAsync(cancellationToken: cancellationToken);

    return products.ToHttpResult();
});
```

## Lifetimes

Use the standard lifetimes:

| Lifetime | Typical use |
| --- | --- |
| `Singleton` | Stateless services and configuration. |
| `Scoped` | Per-request services, such as a `DbContext`. |
| `Transient` | Lightweight, short-lived services. |

`DbContext` is scoped by default with `AddDbContext`. Do not resolve it from a
singleton or store it in a static field.

## Minimal API dependencies

Minimal APIs inject services through parameters that the framework resolves:

```csharp
app.MapGet("/products/{id:int}", async (
    int id,
    CatalogDbContext db,
    CancellationToken cancellationToken) =>
{
    var product = await db.Products.FindAsync([id], cancellationToken);

    return product is null ? Results.NotFound() : Results.Ok(product);
});
```

## MVC controllers

Controllers receive dependencies through constructor injection:

```csharp
[ApiController]
[Route("products")]
public sealed class ProductsController(CatalogDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<Product>>> Get(
        CancellationToken cancellationToken)
    {
        var page = await db.Products
            .OrderBy(product => product.Id)
            .PaginateAsync(cancellationToken: cancellationToken);

        return Ok(page);
    }
}
```

## Testing

Because Laranet does not register hidden services, tests can construct the objects they
need directly. Prefer a real `DbContext` for integration tests and fakes only for
boundaries that the test actually needs.

## Related

- [Core concepts](core-concepts.md)
- [Extensibility](extensibility.md)
- [ASP.NET Core dependency injection](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection)
