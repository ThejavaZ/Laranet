# Quick Start

This page gets a minimal, paginated Web API running with Laranet. It assumes the
`0.1.0` packages are available. Until the first public release, use the
[from-source installation](installation.md#build-from-source).

## 1. Create a project

```bash
laranet new shop-api --with-data --with-efcore --with-aspnetcore
cd shop-api
```

The CLI creates a normal Web API project. Add the SQLite provider so the sample runs
without external infrastructure:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

## 2. Add a model and a context

Create `Models/Product.cs`:

```csharp
namespace ShopApi.Models;

public sealed class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
}
```

Create `Data/ShopDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ShopApi.Models;

namespace ShopApi.Data;

public sealed class ShopDbContext(DbContextOptions<ShopDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
}
```

## 3. Register the context

Replace the contents of `Program.cs`:

```csharp
using Laranet.AspNetCore.Extensions;
using Laranet.Data.Extensions;
using Laranet.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;
using ShopApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShopDbContext>(options =>
    options.UseSqlite("Data Source=shop.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

    await db.Database.EnsureCreatedAsync();

    if (!await db.Products.AnyAsync())
    {
        db.Products.AddRange(
            new Product { Name = "Mechanical keyboard", Price = 120.00m },
            new Product { Name = "Wireless mouse", Price = 45.50m },
            new Product { Name = "4K monitor", Price = 349.00m });

        await db.SaveChangesAsync();
    }
}

app.MapGet("/products", async (
    ShopDbContext db,
    CancellationToken cancellationToken,
    int page = 1,
    int pageSize = 2,
    string? search = null) =>
{
    var result = await db.Products
        .Search(product => product.Name, search)
        .OrderBy(product => product.Id)
        .PaginateAsync(page, pageSize, cancellationToken: cancellationToken);

    return result.ToHttpResult();
});

app.Run();
```

## 4. Run and query

```bash
dotnet run
```

```bash
curl 'http://localhost:5000/products?page=1&pageSize=2'
```

The response body is a `PagedResult<Product>`:

```json
{
  "items": [
    { "id": 1, "name": "Mechanical keyboard", "price": 120.00 },
    { "id": 2, "name": "Wireless mouse", "price": 45.50 }
  ],
  "page": 1,
  "pageSize": 2,
  "totalCount": 3,
  "totalPages": 2,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

The same response includes pagination headers:

```text
X-Total-Count: 3
X-Page: 1
X-Page-Size: 2
X-Total-Pages: 2
```

## What just happened

- `Search` translated the optional search term into a `WHERE` clause.
- `OrderBy` produced a deterministic order before pagination.
- `PaginateAsync` executed a count query and a page query through EF Core.
- `ToHttpResult` kept the native `PagedResult<T>` body and added the HTTP headers.

## Next steps

- [First application](first-application.md) builds a complete catalog API.
- [Data access guide](../guides/data-access.md) covers sorting and conditional filters.
- [Pagination guide](../guides/pagination.md) covers metadata and edge cases.
- [Runnable samples](../../samples) show the same ideas with more detail.
