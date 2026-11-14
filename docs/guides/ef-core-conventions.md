# EF Core conventions

> **Status:** `Laranet.EntityFrameworkCore` ships optional auditing and soft-delete
> conventions. They are opt-in: referencing the package changes no behavior until you
> implement the marker interfaces and register the helpers. See
> [ADR-0009](../architecture/decisions/0009-optional-ef-core-conventions.md).

Laranet keeps `DbContext` as the center of persistence. It does not replace EF Core
conventions for entities, primary keys, relationships, or query filters. The package
adds only the two concerns that need `SaveChanges` interception and a query filter.

## Auditing

Implement `IAuditable` on the entities that need creation and modification timestamps:

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

Register the interceptor where you configure the context:

```csharp
using Laranet.EntityFrameworkCore.Extensions;

optionsBuilder
    .UseSqlServer(connectionString)
    .AddLaranetAuditing();
```

With dependency injection:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseSqlServer(connectionString)
        .AddLaranetAuditing());
```

Behavior:

- When an entity is added, `CreatedAt` is set and `UpdatedAt` is cleared.
- When an entity is modified, `UpdatedAt` is set and `CreatedAt` is not changed.
- Both `SaveChanges` and `SaveChangesAsync` are covered.
- Entities that do not implement `IAuditable` are not touched.

Timestamps come from `TimeProvider.System` by default. Supply a different clock for
tests or controlled environments:

```csharp
optionsBuilder
    .UseSqlServer(connectionString)
    .AddLaranetAuditing(timeProvider);
```

`TimeProvider.GetUtcNow()` returns a UTC value by contract. The conventions store that
value as-is; do not return a non-UTC offset from a custom `TimeProvider` if the
application targets PostgreSQL.

## Soft delete

Implement `ISoftDeletable` and register the interceptor:

```csharp
using Laranet.EntityFrameworkCore.Conventions;

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

Install the query filter in `OnModelCreating`:

```csharp
using Laranet.EntityFrameworkCore.Extensions;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.ApplySoftDeleteQueryFilters();
}
```

Behavior:

- `Remove` converts the `Deleted` entry into an `UPDATE` instead of a `DELETE`.
- `IsDeleted` becomes `true` and `DeletedAt` records the deletion instant.
- Queries exclude soft-deleted rows through a native EF Core query filter.
- `IgnoreQueryFilters()` includes them again.
- When the entity also implements `IAuditable`, `UpdatedAt` records the deletion.
- Entities that do not implement `ISoftDeletable` keep native delete behavior.

Restore an entity by clearing both flags and saving:

```csharp
product.IsDeleted = false;
product.DeletedAt = null;
await dbContext.SaveChangesAsync();
```

Laranet does not cascade soft deletes. If related rows must be deleted together, do it
explicitly in the same unit of work.

## Combining with other query filters

Call `ApplySoftDeleteQueryFilters()` after configuring your own filters. EF Core 10
does not allow anonymous and named query filters on the same entity type, so the helper
adapts to the style already in use:

| Existing filters | Result |
| --- | --- |
| One anonymous filter | Soft delete is combined with it using `AND`. |
| One or more named filters | A named `LaranetSoftDelete` filter is added. |
| None | An anonymous soft-delete filter is installed. |

```csharp
modelBuilder.Entity<Product>()
    .HasQueryFilter(product => product.TenantId == tenantId);

modelBuilder.ApplySoftDeleteQueryFilters();
```

If you add filters after calling the helper, use the same style the helper selected. A
context that mixed anonymous and named filters would be rejected by EF Core.

`IgnoreQueryFilters()` disables every filter on the query, including tenant filters.
Use it deliberately, for example in administrative or restoration code.

## What Laranet does not abstract

- **Entity and primary-key conventions.** EF Core already discovers entities, `Id`,
  `<Type>Id`, and CLR navigation properties.
- **Relationships.** Use ordinary navigation properties and the Fluent API. See
  [Relationships](relationships.md).
- **General global filters.** Call `HasQueryFilter` directly.
- **User stamps.** `CreatedBy` and `UpdatedBy` require request identity integration and
  are not part of these conventions.

## Provider behavior

The conventions are provider-neutral. Timestamp storage depends on the EF Core
provider:

| Provider | `DateTimeOffset` storage | Offset behavior |
| --- | --- | --- |
| SQLite | `TEXT` | Preserved. |
| SQL Server | `datetimeoffset` | Preserved. |
| PostgreSQL | `timestamp with time zone` | Only a zero (UTC) offset is accepted. |
| MySQL and MariaDB through `MySql.EntityFrameworkCore` | `datetime(6)` | Normalized to UTC; the original offset is lost. |

Because the interceptors use `TimeProvider.GetUtcNow()`, the default behavior produces
UTC timestamps that work on every verified provider. The instant is preserved in all
cases; only the offset can change or be rejected. See the
[provider compatibility matrix](../compatibility/providers.md).

## Limitations

- The soft-delete filter is applied to root entity types. Owned types are ignored.
- In an inheritance hierarchy, implement `ISoftDeletable` on the root entity type.
- `IsDeleted`, `DeletedAt`, `CreatedAt`, and `UpdatedAt` must be mapped properties.
- Soft delete is a row update, not a database-level delete.
- There is no archive table, hard-delete escape hatch, or multi-tenancy helper.
- The helpers do not create migrations. Add the mapped properties to your schema as
  usual.

## Related

- [Relationships](relationships.md)
- [Data access](data-access.md)
- [Provider compatibility](../compatibility/providers.md)
- [`Laranet.EntityFrameworkCore` README](../../src/Laranet.EntityFrameworkCore/README.md)
- [ADR-0009](../architecture/decisions/0009-optional-ef-core-conventions.md)
