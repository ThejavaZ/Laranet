# Laranet.Data

Provider-neutral `IQueryable` utilities for search, sorting, and pagination.

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

### Sorting

```csharp
using Laranet.Data.Extensions;
using Laranet.Data.Sorting;

var sorted = dbContext.Products
    .SortBy(product => product.Name, SortDirection.Descending);
```

Dynamic sorting by property name is also available:

```csharp
var sorted = dbContext.Products
    .OrderBy("Name")
    .ThenBy("Id", SortDirection.Descending);
```

Property names are case-sensitive and must refer to a public, readable instance
property on the element type. When the property name comes from external input,
validate it against an allowlist before using it.

### Pagination

```csharp
using Laranet.Data.Extensions;

var page = dbContext.Products
    .OrderBy(product => product.Id)
    .Paginate(page: 2, pageSize: 20);
```

`Paginate` returns a `PagedResult<T>` with the page items, current page, effective
page size, total count, and total page count.

Order the query before paginating. Without a deterministic `ORDER BY`, the
provider may return rows in an unstable order across pages, which can cause
duplicated or missing items. Ordering by a unique key is recommended.

Pagination is synchronous and executes two queries: one for the total count and
one for the requested page. Asynchronous execution is intentionally left to
provider-specific integrations.

### Conditional filtering

```csharp
using Laranet.Data.Extensions;

var products = dbContext.Products
    .WhereIf(!string.IsNullOrWhiteSpace(search), product => product.Name == search);
```

The predicate argument is required even when the condition is `false`.

## Requirements

- .NET 10

## License

Laranet is licensed under the [MIT License](https://github.com/TheJavaZ/Laranet/blob/main/LICENSE).
