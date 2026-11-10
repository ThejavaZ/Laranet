# Data API (`Laranet.Data`)

`Laranet.Data` provides provider-neutral operations over any `IQueryable<T>`. It does
not reference Entity Framework Core, ASP.NET Core, or a database provider.

## Namespaces

| Namespace | Contains |
| --- | --- |
| `Laranet.Data.Extensions` | Extension methods for search, sorting, conditional filtering, and pagination. |
| `Laranet.Data.Pagination` | `PagedResult<T>`. |
| `Laranet.Data.Sorting` | `SortDirection`. |

The extension methods are extension methods on `IQueryable<T>`; import
`Laranet.Data.Extensions` to use them.

## Search

```csharp
public static IQueryable<T> Search<T>(
    this IQueryable<T> source,
    Expression<Func<T, string?>> selector,
    string? term);

public static IQueryable<T> Search<T>(
    this IQueryable<T> source,
    IEnumerable<Expression<Func<T, string?>>> selectors,
    string? term);
```

`Search` filters rows whose selected string property contains a term:

```csharp
var products = db.Products
    .Search(product => product.Name, request.Search);
```

To match a term in any of several properties:

```csharp
var products = db.Products
    .Search(
        [product => product.Name, product => product.Description],
        request.Search);
```

Behavior:

- A `null`, empty, or whitespace-only term leaves the query unchanged.
- The term is captured through a closure so providers can parameterize it instead of
  embedding it as a SQL literal.
- Items whose selected property is `null` never match.
- The multi-column overload combines the selectors with a logical `OR` in a single
  expression tree, so the provider receives one translated query.
- An empty selector collection throws `ArgumentException`. A `null` element throws
  `ArgumentNullException`.
- Case sensitivity, collation, and wildcard behavior are determined by the provider
  and the database configuration. See the
  [provider compatibility](../compatibility/providers.md) page.

## Typed sorting

```csharp
public static IOrderedQueryable<T> SortBy<T, TKey>(
    this IQueryable<T> source,
    Expression<Func<T, TKey>> selector,
    SortDirection direction = SortDirection.Ascending);

public static IOrderedQueryable<T> ThenBy<T, TKey>(
    this IOrderedQueryable<T> source,
    Expression<Func<T, TKey>> selector,
    SortDirection direction = SortDirection.Ascending);
```

`SortBy` starts an ordering; `ThenBy` adds a secondary ordering.

```csharp
var sorted = db.Products
    .SortBy(product => product.Name)
    .ThenBy(product => product.Price, SortDirection.Descending);
```

A direction other than `Ascending` or `Descending` throws
`ArgumentOutOfRangeException`. Null ordering and comparison semantics depend on the
provider.

## Dynamic sorting

Dynamic sorting accepts a property name instead of an expression:

```csharp
public static IOrderedQueryable<T> OrderBy<T>(
    this IQueryable<T> source,
    string propertyName,
    SortDirection direction = SortDirection.Ascending);

public static IOrderedQueryable<T> OrderBy<T>(
    this IQueryable<T> source,
    string propertyName,
    IReadOnlyCollection<string> allowedProperties,
    SortDirection direction = SortDirection.Ascending);

public static IOrderedQueryable<T> ThenBy<T>(
    this IOrderedQueryable<T> source,
    string propertyName,
    SortDirection direction = SortDirection.Ascending);

public static IOrderedQueryable<T> ThenBy<T>(
    this IOrderedQueryable<T> source,
    string propertyName,
    IReadOnlyCollection<string> allowedProperties,
    SortDirection direction = SortDirection.Ascending);
```

Use the allowlist overload when the property name comes from external input:

```csharp
private static readonly string[] SortableProperties =
    ["Name", "Price", "CreatedAt"];

var sorted = db.Products
    .OrderBy(request.SortBy, SortableProperties, request.Direction);
```

Behavior:

- Property names are case-sensitive and must match the CLR property exactly.
- The property must be public, readable, non-static, and not an indexer.
- The allowlist comparison is ordinal and case-sensitive. An empty allowlist rejects
  every property.
- Invalid names throw `ArgumentException` before the query executes.
- The resulting ordering is translated by the provider. Computed or unmapped
  properties may fail at execution time depending on provider capabilities.

## Conditional filtering

```csharp
public static IQueryable<T> WhereIf<T>(
    this IQueryable<T> source,
    bool condition,
    Expression<Func<T, bool>> predicate);
```

```csharp
var products = db.Products
    .WhereIf(request.MinPrice.HasValue,
        product => product.Price >= request.MinPrice!.Value)
    .WhereIf(request.OnlyActive == true,
        product => product.IsActive);
```

`WhereIf` returns the filtered query when the condition is `true` and the original
query otherwise. The predicate argument is required even when the condition is
`false`.

## Pagination

```csharp
public const int DefaultPageSize = 15;
public const int DefaultMaxPageSize = 100;

public static PagedResult<T> Paginate<T>(
    this IQueryable<T> source,
    int page = 1,
    int pageSize = DefaultPageSize,
    int maxPageSize = DefaultMaxPageSize);
```

```csharp
var page = db.Products
    .OrderBy(product => product.Id)
    .Paginate(page: 2, pageSize: 20);
```

Behavior:

- `page`, `pageSize`, and `maxPageSize` must be at least `1`.
- The effective page size is the smaller of `pageSize` and `maxPageSize`.
- A page beyond the available results returns an empty `Items` collection while
  preserving the total count.
- When the query is empty or the requested page is out of range, only the count query
  is executed.
- `Paginate` is synchronous and executes a count query plus the page query. Use
  `PaginateAsync` from `Laranet.EntityFrameworkCore` for asynchronous execution.
- Order the query before paginating. Without a deterministic `ORDER BY`, the provider
  does not guarantee a stable row order, and items may be duplicated or omitted
  across pages.

## `PagedResult<T>`

```csharp
namespace Laranet.Data.Pagination;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage { get; }
    public bool HasNextPage { get; }

    public PagedResult(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalCount);
}
```

| Property | Meaning |
| --- | --- |
| `Items` | Items in the current page. |
| `Page` | Current page number, starting at 1. |
| `PageSize` | Effective page size after applying the maximum. |
| `TotalCount` | Total items across all pages. |
| `TotalPages` | `ceil(TotalCount / PageSize)`. `0` when there are no items. |
| `HasPreviousPage` | `Page > 1`. |
| `HasNextPage` | `Page < TotalPages`. |

The constructor throws `ArgumentNullException` when `items` is `null`, and
`ArgumentOutOfRangeException` when `page` or `pageSize` is less than `1`, or when
`totalCount` is negative.

## `SortDirection`

```csharp
namespace Laranet.Data.Sorting;

public enum SortDirection
{
    Ascending,
    Descending
}
```

## Composing operations

All operations preserve query composition. A typical list endpoint combines them in a
single expression tree:

```csharp
var page = dbContext.Products
    .WhereIf(request.MinPrice.HasValue,
        product => product.Price >= request.MinPrice!.Value)
    .WhereIf(request.OnlyActive == true,
        product => product.IsActive)
    .Search(
        [product => product.Name, product => product.Description],
        request.Search)
    .SortBy(product => product.Name)
    .Paginate(request.Page, request.PageSize);
```

The provider translates the final expression. Laranet does not materialize the query
prematurely or switch to in-memory execution.

## Related

- [Data access guide](../guides/data-access.md)
- [Pagination guide](../guides/pagination.md)
- [Provider compatibility](../compatibility/providers.md)
- [`Laranet.Data` README](../../src/Laranet.Data/README.md)
