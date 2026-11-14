# Integration API (`Laranet.EntityFrameworkCore` and `Laranet.AspNetCore`)

The integration packages add framework-specific behavior without changing the
provider-neutral contract of `Laranet.Data`.

## Entity Framework Core

### Namespace

`Laranet.EntityFrameworkCore.Extensions`

### `PaginateAsync`

```csharp
public static Task<PagedResult<T>> PaginateAsync<T>(
    this IQueryable<T> source,
    int page = 1,
    int pageSize = QueryablePaginationExtensions.DefaultPageSize,
    int maxPageSize = QueryablePaginationExtensions.DefaultMaxPageSize,
    CancellationToken cancellationToken = default);
```

```csharp
using Laranet.Data.Extensions;
using Laranet.EntityFrameworkCore.Extensions;

var page = await db.Products
    .Search(product => product.Name, request.Search)
    .OrderBy(product => product.Id)
    .PaginateAsync(
        page: request.Page,
        pageSize: request.PageSize,
        cancellationToken: cancellationToken);
```

Behavior:

- Async counterpart of `Paginate`, returning the same `PagedResult<T>` type.
- Executes `CountAsync` and, when needed, `ToListAsync` through EF Core.
- When the query is empty or the requested page is out of range, only the count
  query is executed.
- The effective page size is the smaller of `pageSize` and `maxPageSize`.
- Order the query before paginating. Without a deterministic `ORDER BY`, rows may be
  duplicated or omitted across pages.
- `cancellationToken` cancels the operation and surfaces as
  `OperationCanceledException`.
- Invalid arguments throw `ArgumentNullException` or `ArgumentOutOfRangeException`
  before any command executes.

Asynchronous execution lives in this package so that `Laranet.Data` remains
independent of EF Core.

### Conventions namespaces

- `Laranet.EntityFrameworkCore.Conventions`
- `Laranet.EntityFrameworkCore.Extensions`

### `IAuditable`

```csharp
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
}
```

### `ISoftDeletable`

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}
```

### `AddLaranetAuditing`

```csharp
public static DbContextOptionsBuilder AddLaranetAuditing(
    this DbContextOptionsBuilder optionsBuilder,
    TimeProvider? timeProvider = null);

public static DbContextOptionsBuilder<TContext> AddLaranetAuditing<TContext>(
    this DbContextOptionsBuilder<TContext> optionsBuilder,
    TimeProvider? timeProvider = null)
    where TContext : DbContext;
```

Registers a `SaveChanges` interceptor that sets `CreatedAt` for added entities and
`UpdatedAt` for modified entities that implement `IAuditable`. The default clock is
`TimeProvider.System`.

### `AddLaranetSoftDelete`

```csharp
public static DbContextOptionsBuilder AddLaranetSoftDelete(
    this DbContextOptionsBuilder optionsBuilder,
    TimeProvider? timeProvider = null);

public static DbContextOptionsBuilder<TContext> AddLaranetSoftDelete<TContext>(
    this DbContextOptionsBuilder<TContext> optionsBuilder,
    TimeProvider? timeProvider = null)
    where TContext : DbContext;
```

Registers a `SaveChanges` interceptor that converts deletions of `ISoftDeletable`
entities into updates of `IsDeleted` and `DeletedAt`. When the entity also implements
`IAuditable`, `UpdatedAt` is updated too.

### `ApplySoftDeleteQueryFilters`

```csharp
public static ModelBuilder ApplySoftDeleteQueryFilters(
    this ModelBuilder modelBuilder);
```

Installs a native EF Core query filter for root entity types that implement
`ISoftDeletable`. The filter composes with an existing anonymous filter or with named
filters. Call it after configuring your own filters and use `IgnoreQueryFilters()` to
include soft-deleted rows in a query.

See the [EF Core conventions guide](../guides/ef-core-conventions.md) for usage,
limitations, and provider behavior.

## ASP.NET Core

### Namespace

`Laranet.AspNetCore.Extensions`

### `WritePaginationHeaders`

```csharp
public static void WritePaginationHeaders<T>(
    this PagedResult<T> result,
    HttpResponse response);
```

Writes the pagination metadata to the response headers:

| Header | Value |
| --- | --- |
| `X-Total-Count` | `TotalCount` |
| `X-Page` | `Page` |
| `X-Page-Size` | `PageSize` |
| `X-Total-Pages` | `TotalPages` |

### `ToHttpResult`

```csharp
public static IResult ToHttpResult<T>(this PagedResult<T> result);
```

Creates an `IResult` for Minimal API endpoints. The body keeps the native
`PagedResult<T>` JSON shape and the pagination headers are added automatically.

```csharp
using Laranet.AspNetCore.Extensions;

app.MapGet("/products", async (
    CatalogDbContext db,
    [AsParameters] ProductListRequest request,
    CancellationToken cancellationToken) =>
{
    var page = await db.Products
        .Search(product => product.Name, request.Search)
        .OrderBy(product => product.Id)
        .PaginateAsync(request.Page, request.PageSize, cancellationToken: cancellationToken);

    return page.ToHttpResult();
});
```

The response looks like this:

```text
HTTP/1.1 200 OK
Content-Type: application/json
X-Total-Count: 42
X-Page: 2
X-Page-Size: 10
X-Total-Pages: 5

{
  "items": [ ... ],
  "page": 2,
  "pageSize": 10,
  "totalCount": 42,
  "totalPages": 5,
  "hasPreviousPage": true,
  "hasNextPage": true
}
```

For MVC controllers, call `WritePaginationHeaders` on `Response` and return the
result normally:

```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<Product>>> Get(
    [FromQuery] ProductListRequest request,
    CancellationToken cancellationToken)
{
    var page = await db.Products
        .OrderBy(product => product.Id)
        .PaginateAsync(request.Page, request.PageSize, cancellationToken: cancellationToken);

    page.WritePaginationHeaders(Response);

    return Ok(page);
}
```

## Dependencies

| Package | References |
| --- | --- |
| `Laranet.EntityFrameworkCore` | `Laranet.Data`, `Microsoft.EntityFrameworkCore` |
| `Laranet.AspNetCore` | `Laranet.Data`, ASP.NET Core shared framework |

Neither package adds EF Core or ASP.NET Core to `Laranet.Data`.

## Related

- [Data API](data.md)
- [Pagination guide](../guides/pagination.md)
- [EF Core conventions guide](../guides/ef-core-conventions.md)
- [`Laranet.EntityFrameworkCore` README](../../src/Laranet.EntityFrameworkCore/README.md)
- [`Laranet.AspNetCore` README](../../src/Laranet.AspNetCore/README.md)
