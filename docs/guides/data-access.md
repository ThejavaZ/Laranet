# Data Access

`Laranet.Data` adds composable query operations to any `IQueryable<T>`. This guide
shows the common combinations. The [package README](../../src/Laranet.Data/README.md)
is the reference for exact behavior and XML documentation.

## Namespaces

```csharp
using Laranet.Data.Extensions;
using Laranet.Data.Sorting;
```

## Search

`Search` filters rows whose selected string property contains a term:

```csharp
var products = db.Products
    .Search(product => product.Name, request.Search);
```

- A null, empty, or whitespace term leaves the query unchanged.
- The term is sent to the provider as a parameter, not embedded as SQL text.
- Null property values never match.
- Case sensitivity and collation depend on the provider and database configuration.

## Typed sorting

Sort by a property with an explicit direction:

```csharp
var sorted = db.Products
    .SortBy(product => product.Name, SortDirection.Descending);
```

Add a secondary order with `ThenBy`:

```csharp
var sorted = db.Products
    .SortBy(product => product.Name)
    .ThenBy(product => product.Id, SortDirection.Descending);
```

## Dynamic sorting

When the property name comes from a request, use the allowlist overload:

```csharp
private static readonly string[] SortableColumns = ["Name", "Price"];

var sorted = db.Products
    .OrderBy(request.SortBy ?? "Name", SortableColumns, request.SortDirection);
```

The allowlist is validated before the ordering expression is built or any SQL runs.
Comparisons are ordinal and case-sensitive. An empty allowlist rejects every
property, and a missing allowlist throws `ArgumentNullException`.

Without an allowlist, the property name still has to identify a public, readable,
non-static instance property on the element type:

```csharp
var sorted = db.Products.OrderBy("Name");
```

Do not use the unvalidated overload with untrusted input.

## Conditional filtering

`WhereIf` applies a predicate only when a condition is true:

```csharp
var minPrice = request.MinPrice; // decimal?

var products = db.Products
    .WhereIf(minPrice.HasValue, product => product.Price >= minPrice!.Value);
```

The predicate is required even when the condition is false, which keeps the call site
readable:

```csharp
var products = db.Products
    .WhereIf(request.OnlyActive, product => product.IsActive)
    .WhereIf(request.CategoryId.HasValue, product => product.CategoryId == request.CategoryId);
```

## Composition

The extensions preserve the expression tree, so a provider receives one query:

```csharp
private static readonly string[] SortableColumns = ["Name", "Price"];

var minPrice = request.MinPrice; // decimal?

var page = db.Products
    .Search(product => product.Name, request.Search)
    .WhereIf(minPrice.HasValue, product => product.Price >= minPrice!.Value)
    .WhereIf(request.OnlyActive, product => product.IsActive)
    .OrderBy(request.SortBy ?? "Name", SortableColumns, request.SortDirection)
    .Paginate(request.Page, request.PageSize);
```

Avoid materializing the query between operations. Do not call `ToList()` or
`AsEnumerable()` unless you intentionally want in-memory execution.

## Provider behavior

`Laranet.Data` does not reference EF Core, ASP.NET Core, or a database provider. The
provider decides how the expressions are translated and executed.

Behavior that depends on the provider includes:

- String comparison, case sensitivity, and collation for `Search`.
- Null ordering and collation for sorting.
- Translation of computed or unmapped properties.
- Stability of pagination without a deterministic order.

See [Provider Compatibility](../compatibility/providers.md) for the verified
matrix.

## Next steps

- [Pagination](pagination.md)
- [Package overview](../packages/README.md)
