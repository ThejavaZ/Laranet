# Laranet.EntityFrameworkCore

Entity Framework Core integration for `Laranet.Data` asynchronous query execution.

This package adds Entity Framework Core-specific operations without coupling
`Laranet.Data` to Entity Framework Core. It references `Laranet.Data` and the
`Microsoft.EntityFrameworkCore` package.

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

## Requirements

- .NET 10
- Entity Framework Core 10.x

## Provider compatibility

`Laranet.EntityFrameworkCore` relies on Entity Framework Core for translation and
execution. The integration tests verify asynchronous pagination with SQLite. Other
EF Core providers are expected to work when they support the same query operators,
but they are not verified in the current CI matrix.

See [Provider Compatibility](https://github.com/TheJavaZ/Laranet/blob/main/docs/compatibility/providers.md)
for the current support matrix.

## License

Laranet is licensed under the [MIT License](https://github.com/TheJavaZ/Laranet/blob/main/LICENSE).
