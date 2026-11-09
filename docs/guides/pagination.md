# Pagination

Laranet provides synchronous pagination in `Laranet.Data` and asynchronous
pagination in `Laranet.EntityFrameworkCore`. Both return the same
`PagedResult<T>` type.

## Synchronous pagination

```csharp
using Laranet.Data.Extensions;

var page = db.Products
    .OrderBy(product => product.Id)
    .Paginate(page: 2, pageSize: 20);
```

`Paginate` is provider-neutral. It builds `Count`, `Skip`, and `Take` operations and
lets the underlying `IQueryable` provider execute them.

## Asynchronous pagination

```csharp
using Laranet.Data.Extensions;
using Laranet.EntityFrameworkCore.Extensions;

var page = await db.Products
    .OrderBy(product => product.Id)
    .PaginateAsync(page: 2, pageSize: 20, cancellationToken: cancellationToken);
```

`PaginateAsync` lives in the EF Core integration so that `Laranet.Data` stays
independent of EF Core.

## The result

| Property | Meaning |
| --- | --- |
| `Items` | Items in the current page. |
| `Page` | Current page number, starting at 1. |
| `PageSize` | Effective page size after applying the maximum. |
| `TotalCount` | Total items across all pages. |
| `TotalPages` | Total available pages. `0` when there are no items. |
| `HasPreviousPage` | `Page > 1`. |
| `HasNextPage` | `Page < TotalPages`. |

## Order before paginating

Always add a deterministic `ORDER BY` before paginating:

```csharp
var page = await db.Products
    .OrderBy(product => product.Id)
    .PaginateAsync(page, pageSize);
```

Without a deterministic order, the provider may return rows in a different order for
each query, which can duplicate or skip items across pages. Ordering by a unique key
is the safest choice.

## Page size limits

Both methods accept a `maxPageSize` argument:

```csharp
var page = await db.Products
    .OrderBy(product => product.Id)
    .PaginateAsync(page, pageSize: 500, maxPageSize: 100);
```

The effective `PageSize` is the smaller of `pageSize` and `maxPageSize`. The default
maximum is 100 and the default page size is 15.

## Empty results and out-of-range pages

When the query is empty, or the requested page is beyond the last page:

- `Items` is an empty collection.
- `Page`, `PageSize`, and `TotalCount` remain accurate.
- Only the count query is executed.

This avoids an unnecessary page query.

## HTTP integration

`Laranet.AspNetCore` exposes pagination metadata through headers without changing the
response body:

```csharp
using Laranet.AspNetCore.Extensions;

app.MapGet("/products", async (
    CatalogDbContext db,
    CancellationToken cancellationToken,
    int page = 1,
    int pageSize = 20) =>
{
    var result = await db.Products
        .OrderBy(product => product.Id)
        .PaginateAsync(page, pageSize, cancellationToken: cancellationToken);

    return result.ToHttpResult();
});
```

Headers written by `ToHttpResult` and `WritePaginationHeaders`:

| Header | Meaning |
| --- | --- |
| `X-Total-Count` | Total number of items. |
| `X-Page` | Current page number. |
| `X-Page-Size` | Effective page size. |
| `X-Total-Pages` | Total number of pages. |

In MVC controllers, write the headers before returning the body:

```csharp
result.WritePaginationHeaders(Response);
return Ok(result);
```

## Cancellation

`PaginateAsync` accepts a `CancellationToken` for both the count and page queries:

```csharp
var page = await db.Products
    .OrderBy(product => product.Id)
    .PaginateAsync(page, pageSize, cancellationToken: cancellationToken);
```

Pass the request cancellation token from Minimal APIs or controllers.

## Next steps

- [Data access](data-access.md)
- [Provider compatibility](../compatibility/providers.md)
- [Pagination sample](../../samples/Laranet.Samples.WebApi)
