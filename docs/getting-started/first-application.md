# First Application

This walkthrough builds a small catalog API that lists, searches, sorts, filters,
paginates, and creates products. The complete, runnable version is the
[`Laranet.Samples.WebApi`](../../samples/Laranet.Samples.WebApi) sample.

## 1. Create the project

```bash
laranet new catalog-api --layout repository --with-data --with-efcore --with-aspnetcore
cd catalog-api
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

The `repository` layout places the project under `src/catalog-api/` and creates the
solution at the repository root.

## 2. Define the domain

Create `src/catalog-api/Models/Product.cs`:

```csharp
namespace CatalogApi.Models;

public sealed class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

Create `src/catalog-api/Contracts/CreateProductRequest.cs`:

```csharp
namespace CatalogApi.Contracts;

public sealed record CreateProductRequest(string Name, decimal Price);
```

## 3. Configure EF Core

Create `src/catalog-api/Data/CatalogDbContext.cs`:

```csharp
using CatalogApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogApi.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(product => product.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(product => product.Price)
                .HasPrecision(18, 2);

            // SQLite does not support ORDER BY on DateTimeOffset, and DateTime
            // loses its Kind when round-tripped. Store UTC and restore the Kind.
            entity.Property(product => product.CreatedAt)
                .HasConversion(
                    value => value.ToUniversalTime(),
                    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        });
    }
}
```

## 4. Build the API

Replace `src/catalog-api/Program.cs` with:

```csharp
using CatalogApi.Contracts;
using CatalogApi.Data;
using CatalogApi.Models;
using Laranet.AspNetCore.Extensions;
using Laranet.Data.Extensions;
using Laranet.Data.Sorting;
using Laranet.EntityFrameworkCore.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Keep one connection open so the in-memory database survives for the app lifetime.
var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

builder.Services.AddSingleton(connection);
builder.Services.AddDbContext<CatalogDbContext>((services, options) =>
    options.UseSqlite(services.GetRequiredService<SqliteConnection>()));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

    await db.Database.EnsureCreatedAsync();

    if (!await db.Products.AnyAsync())
    {
        db.Products.AddRange(
            new Product { Name = "Mechanical keyboard", Price = 120.00m, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Name = "Wireless mouse", Price = 45.50m, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Name = "4K monitor", Price = 349.00m, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Name = "Laptop stand", Price = 35.00m, IsActive = false, CreatedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();
    }
}

var sortableColumns = new[]
{
    nameof(Product.Name),
    nameof(Product.Price),
    nameof(Product.CreatedAt),
};

app.MapGet("/products", async (
    CatalogDbContext db,
    CancellationToken cancellationToken,
    int page = 1,
    int pageSize = 10,
    string? search = null,
    decimal? minPrice = null,
    bool? onlyActive = null,
    string? sortBy = null,
    SortDirection direction = SortDirection.Ascending) =>
{
    var sortProperty = sortBy ?? nameof(Product.Name);

    if (!sortableColumns.Contains(sortProperty, StringComparer.Ordinal))
    {
        return Results.BadRequest(new { error = $"Unknown sort property '{sortProperty}'." });
    }

    var minPriceValue = minPrice.GetValueOrDefault();

    var result = await db.Products
        .Search(product => product.Name, search)
        .WhereIf(minPrice.HasValue, product => product.Price >= minPriceValue)
        .WhereIf(onlyActive == true, product => product.IsActive)
        .OrderBy(sortProperty, sortableColumns, direction)
        .PaginateAsync(page, pageSize, cancellationToken: cancellationToken);

    return result.ToHttpResult();
});

app.MapGet("/products/{id:int}", async (
    int id,
    CatalogDbContext db,
    CancellationToken cancellationToken) =>
{
    var product = await db.Products.FindAsync([id], cancellationToken);

    return product is null ? Results.NotFound() : Results.Ok(product);
});

app.MapPost("/products", async (
    CreateProductRequest request,
    CatalogDbContext db,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["name"] = new[] { "Name is required." },
        });
    }

    if (request.Price < 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["price"] = new[] { "Price cannot be negative." },
        });
    }

    var product = new Product
    {
        Name = request.Name.Trim(),
        Price = request.Price,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
    };

    db.Products.Add(product);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/products/{product.Id}", product);
});

app.Run();
```

## 5. Run it

```bash
dotnet run --project src/catalog-api/catalog-api.csproj
```

List the first page:

```bash
curl 'http://localhost:5000/products?page=1&pageSize=2'
```

Search, filter, and sort:

```bash
curl 'http://localhost:5000/products?search=monitor&minPrice=100&sortBy=Price&direction=Descending'
```

Create a product:

```bash
curl -X POST http://localhost:5000/products \
  -H 'Content-Type: application/json' \
  -d '{"name":"USB-C hub","price":79.99}'
```

## 6. What to notice

- Pagination does not require a custom response envelope. The body is
  `PagedResult<T>` and the metadata is also available through headers.
- `WhereIf` keeps optional filters in the query pipeline without branching.
- Dynamic sorting uses an allowlist, so a query-string value cannot select an
  arbitrary property.
- Validation uses native ASP.NET Core results. Laranet does not add an alternative
  validation framework.
- The generated project is ordinary C#. Nothing in the application depends on
  Laranet at runtime except the packages you explicitly reference.

## Next steps

- [Data access guide](../guides/data-access.md)
- [Pagination guide](../guides/pagination.md)
- [CLI guide](../guides/cli.md)
