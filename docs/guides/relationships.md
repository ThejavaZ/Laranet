# Relationships

> **Status:** Laranet 0.1.0 does not ship relationship abstractions. This guide shows
> the native Entity Framework Core approach that works with the current packages. The
> roadmap evaluates optional declarative conventions in Version 3. See
> [ADR-0006](../architecture/decisions/0006-toolkit-with-framework-tooling.md) and the
> [Version 3 roadmap](../roadmap/v3.md).

## Declare relationships with EF Core

Laranet does not generate `HasMany<T>` or `BelongsTo<T>` wrappers. EF Core recognizes
CLR navigation properties and collections, so relationships are declared with ordinary
C# and configured with the Fluent API when conventions are not enough.

### One-to-many

```csharp
public sealed class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}

public sealed class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}
```

The `CategoryId` property follows the EF Core foreign-key convention. When the names do
not follow the convention, configure them explicitly:

```csharp
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### One-to-one

```csharp
modelBuilder.Entity<Product>()
    .HasOne(product => product.Details)
    .WithOne(details => details.Product)
    .HasForeignKey<ProductDetails>(details => details.ProductId);
```

### Many-to-many

```csharp
public sealed class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Tag> Tags { get; set; } = [];
}

public sealed class Tag
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}
```

EF Core creates the join table by convention. Configure it explicitly when you need
extra columns or a specific table name.

## Loading related data

### Eager loading

Use `Include` and `ThenInclude` when the response needs the related data:

```csharp
var products = await db.Products
    .Include(product => product.Category)
    .OrderBy(product => product.Name)
    .ToListAsync(cancellationToken);
```

`Include` returns an `IIncludableQueryable<TEntity, TProperty>`, which still implements
`IQueryable<T>`. Laranet extensions therefore compose with it:

```csharp
var page = await db.Products
    .Include(product => product.Category)
    .Search(product => product.Name, request.Search)
    .OrderBy(product => product.Id)
    .PaginateAsync(request.Page, request.PageSize, cancellationToken: cancellationToken);
```

### Projection

Projection is often better than eager loading. It selects only the columns the API
returns and avoids materializing full entities:

```csharp
var page = await db.Products
    .OrderBy(product => product.Name)
    .Select(product => new ProductResponse(
        product.Id,
        product.Name,
        product.Price,
        product.Category!.Name))
    .PaginateAsync(request.Page, request.PageSize, cancellationToken: cancellationToken);
```

### Explicit loading

Load a navigation property after the entity is already tracked:

```csharp
var category = await db.Categories.FindAsync([id], cancellationToken);

if (category is not null)
{
    await db.Entry(category)
        .Collection(item => item.Products)
        .LoadAsync(cancellationToken);
}
```

## Avoid N+1 queries

Accessing a navigation property that was not loaded in a loop issues one query per
item. Prefer `Include` or, better, a projection:

```csharp
// Avoid: one query per product.
foreach (var product in await db.Products.ToListAsync(cancellationToken))
{
    Console.WriteLine(product.Category?.Name);
}

// Prefer: a single projected query.
var rows = await db.Products
    .Select(product => new { product.Name, Category = product.Category!.Name })
    .ToListAsync(cancellationToken);
```

## Nullable navigation properties

Use nullable navigation properties (`Category?`) when the relationship is optional.
Use a required collection initialized to an empty list (`= []`) so callers do not need
null checks. Keep nullable reference types enabled.

## Tracking versus no-tracking

Read-only endpoints do not need change tracking:

```csharp
var products = await db.Products
    .AsNoTracking()
    .OrderBy(product => product.Name)
    .ToListAsync(cancellationToken);
```

Apply `AsNoTracking()` before composing other operators.

## What Laranet adds today

Nothing beyond the native EF Core behavior:

- `Search`, `WhereIf`, `SortBy`, `OrderBy`, and `Paginate` operate on any
  `IQueryable<T>`, including queries that use `Include`.
- `PaginateAsync` executes the query asynchronously through EF Core.

The Version 3 roadmap studies optional conventions such as auditing and soft delete.
Active Record-style relationship wrappers are explicitly deferred until an ADR and a
concrete use case exist.

## Related

- [Data access](data-access.md)
- [Pagination](pagination.md)
- [Version 3 roadmap](../roadmap/v3.md)
- [EF Core relationships](https://learn.microsoft.com/ef/core/modeling/relationships)
