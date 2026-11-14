# Laranet.EntityFrameworkCore

Entity Framework Core integration for asynchronous query execution and optional
conventions.

This package adds Entity Framework Core-specific operations without coupling
`Laranet.Data` to Entity Framework Core. It references `Laranet.Data` and the
`Microsoft.EntityFrameworkCore` package.

The package provides:

- `PaginateAsync` for asynchronous pagination.
- Opt-in `CreatedAt` / `UpdatedAt` auditing through `IAuditable`.
- Opt-in soft delete through `ISoftDeletable`, a `SaveChanges` interceptor, and a
  native global query filter.

## Installation

```bash
dotnet add package Laranet.EntityFrameworkCore
```

## Usage

```csharp
using Laranet.Data.Extensions;
using Laranet.EntityFrameworkCore.Extensions;

var page = await dbContext.Products
    .OrderBy(product => product.Id)
    .PaginateAsync(page: 2, pageSize: 20);
```

`PaginateAsync` returns a `PagedResult<T>` with the page items, current page,
effective page size, total count, and total page count.

Order the query before paginating. Without a deterministic `ORDER BY`, the provider
may return rows in an unstable order across pages, which can cause duplicated or
missing items. Ordering by a unique key is recommended.

When the collection is empty or the requested page is out of range, only the count
query is executed and an empty page is returned with correct metadata.

The method accepts a `CancellationToken` for the count and page queries:

```csharp
var page = await dbContext.Products
    .OrderBy(product => product.Id)
    .PaginateAsync(page: 1, pageSize: 20, cancellationToken: cancellationToken);
```

`PaginateAsync` is composable with the existing `Laranet.Data` extensions:

```csharp
var page = await dbContext.Products
    .Search(product => product.Name, searchTerm)
    .SortBy(product => product.Name)
    .PaginateAsync(page, pageSize, cancellationToken: cancellationToken);
```

## Conventions

The conventions are opt-in. Nothing changes until an entity implements a marker
interface and the corresponding helper is registered.

### Auditing

```csharp
using Laranet.EntityFrameworkCore.Conventions;

public sealed class Product : IAuditable
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
```

```csharp
using Laranet.EntityFrameworkCore.Extensions;

optionsBuilder
    .UseSqlServer(connectionString)
    .AddLaranetAuditing();
```

`CreatedAt` is set when the entity is added. `UpdatedAt` is set when it is modified and
stays `null` until then. Both `SaveChanges` and `SaveChangesAsync` are covered.
Timestamps use `TimeProvider.System`; pass a `TimeProvider` to
`AddLaranetAuditing` to control the clock.

### Soft delete

```csharp
public sealed class Product : ISoftDeletable
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
```

```csharp
optionsBuilder
    .UseSqlServer(connectionString)
    .AddLaranetSoftDelete();
```

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.ApplySoftDeleteQueryFilters();
}
```

`Remove` updates `IsDeleted` and `DeletedAt` instead of deleting the row. Queries
exclude soft-deleted rows by default; use `IgnoreQueryFilters()` to include them. When
the entity also implements `IAuditable`, `UpdatedAt` records the deletion. Call
`ApplySoftDeleteQueryFilters()` after configuring your own filters; it adapts to
anonymous or named EF Core query filters.

See the [EF Core conventions guide](https://github.com/TheJavaZ/Laranet/blob/main/docs/guides/ef-core-conventions.md)
for restoration, filter composition, limitations, and provider differences.

## Requirements

- .NET 10
- Entity Framework Core 10.x

## Provider compatibility

`Laranet.EntityFrameworkCore` relies on Entity Framework Core for translation and
execution. Pagination and conventions are verified with SQLite, SQL Server,
PostgreSQL, MySQL, and MariaDB.

`DateTimeOffset` storage depends on the provider. Laranet stores UTC timestamps, which
work on every verified provider. PostgreSQL rejects a non-UTC offset, and MySQL
normalizes timestamps to UTC.

See [Provider Compatibility](https://github.com/TheJavaZ/Laranet/blob/main/docs/compatibility/providers.md)
for the current support matrix and provider-specific behavior.

## License

Laranet is licensed under the [MIT License](https://github.com/TheJavaZ/Laranet/blob/main/LICENSE).
