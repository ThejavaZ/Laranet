# Laranet.Data

Provider-neutral `IQueryable` utilities for search, sorting, conditional filtering, and
pagination.

`Laranet.Data` extends LINQ queries without taking a dependency on Entity Framework
Core, ASP.NET Core, or any specific database provider. The extensions compose with
any `IQueryable<T>` implementation and leave query execution to the underlying
provider.

## Installation

```bash
dotnet add package Laranet.Data
```

## Usage

### Search

```csharp
using Laranet.Data.Extensions;

var products = dbContext.Products
    .Search(product => product.Name, "keyboard");
```

`Search` filters out null values and applies `string.Contains` to the selected
property. The comparison follows the case sensitivity and collation of the
underlying provider. Empty or whitespace-only terms leave the query unchanged.

The search term is sent to the provider as a parameter rather than embedded in the
command text.

### Sorting

```csharp
using Laranet.Data.Extensions;
using Laranet.Data.Sorting;

var sorted = dbContext.Products
    .SortBy(product => product.Name, SortDirection.Descending);
```

Secondary ordering is available through `ThenBy`:

```csharp
var sorted = dbContext.Products
    .SortBy(product => product.Name)
    .ThenBy(product => product.Id, SortDirection.Descending);
```

Dynamic sorting by property name is also available:

```csharp
var sorted = dbContext.Products
    .OrderBy("Name")
    .ThenBy("Id", SortDirection.Descending);
```

Property names are case-sensitive and must refer to a public, readable instance
property on the element type.

When the property name comes from external input, pass an allowlist. The overload
validates the name before building the ordering expression or executing any SQL:

```csharp
private static readonly string[] SortableColumns = ["Name", "Price"];

var sorted = dbContext.Products
    .OrderBy("Price", SortableColumns, SortDirection.Descending);
```

Allowlist comparisons are ordinal and case-sensitive. A `null` allowlist throws
`ArgumentNullException`; an empty allowlist rejects every property. Names outside the
allowlist throw `ArgumentException` before the query is built.

### Conditional filtering

`WhereIf` applies a predicate only when its condition is `true`, which keeps optional
filters readable without branching the query:

```csharp
using Laranet.Data.Extensions;

var minPrice = request.MinPrice; // decimal?

var products = dbContext.Products
    .WhereIf(minPrice.HasValue, product => product.Price >= minPrice.Value);
```

When `minPrice` is `null`, the query is returned unchanged; when it has a value, the
price filter is translated and executed by the provider. The predicate argument is
required even when the condition is `false`.

### Composition

The extensions are designed to be chained. A typical query combines an optional
search term, optional filters, an allowlisted sort, and pagination:

```csharp
private static readonly string[] SortableColumns = ["Name", "Price"];

var minPrice = request.MinPrice; // decimal?

var page = dbContext.Products
    .Search(product => product.Name, request.Search)
    .WhereIf(
        minPrice.HasValue,
        product => product.Price >= minPrice.Value)
    .WhereIf(
        request.OnlyActive,
        product => product.IsActive)
    .OrderBy(
        request.SortBy ?? "Name",
        SortableColumns,
        request.SortDirection)
    .Paginate(request.Page, request.PageSize);
```

Composition preserves the expression tree, so the provider receives a single query
instead of multiple in-memory passes.

### Pagination

```csharp
using Laranet.Data.Extensions;

var page = dbContext.Products
    .OrderBy(product => product.Id)
    .Paginate(page: 2, pageSize: 20);
```

`Paginate` returns a `PagedResult<T>` with the page items, current page, effective
page size, total count, and total page count.

Order the query before paginating. Without a deterministic `ORDER BY`, the provider
may return rows in an unstable order across pages, which can cause duplicated or
missing items. Ordering by a unique key is recommended.

Pagination is synchronous and normally executes two queries: one for the total count
and one for the requested page. When the collection is empty or the requested page is
out of range, only the count query is executed and an empty page is returned with
correct metadata.

Asynchronous execution is intentionally left to provider-specific integrations.

## Provider compatibility

The query building APIs are provider-neutral and rely on standard `Queryable` methods
and expression trees. The test suite verifies translation and execution with SQLite
through Entity Framework Core. Other providers are expected to work when they support
the same standard query operators, but they are not verified in the current CI matrix.

See [Provider Compatibility](https://github.com/TheJavaZ/Laranet/blob/main/docs/compatibility/providers.md)
for the current support matrix and provider-specific expectations.

## Requirements

- .NET 10

## License

Laranet is licensed under the [MIT License](https://github.com/TheJavaZ/Laranet/blob/main/LICENSE).
