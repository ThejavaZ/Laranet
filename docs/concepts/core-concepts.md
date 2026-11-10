# Core Concepts

Laranet is small on purpose. These are the ideas that explain how the packages fit
together and why they behave the way they do.

## `IQueryable` is the contract

The data utilities are extension methods over `IQueryable<T>`. They build standard
`Queryable` expression trees and delegate execution to the underlying provider.

This has three consequences:

1. **Composition is free.** Search, filtering, sorting, and pagination combine in a
   single query.
2. **No premature materialization.** Laranet does not call `ToList()` until pagination
   needs the page items.
3. **Provider neutrality.** The provider decides how expressions become SQL or
   in-memory operations.

```csharp
var page = db.Products
    .WhereIf(request.OnlyActive == true, product => product.IsActive)
    .Search(product => product.Name, request.Search)
    .SortBy(product => product.Name)
    .Paginate(request.Page, request.PageSize);
```

## Provider neutrality

`Laranet.Data` does not assume that every provider behaves like Entity Framework Core.
Behavior that requires a specific framework lives in an integration package. For
example, `Paginate` is synchronous and provider-neutral, while `PaginateAsync` lives
in `Laranet.EntityFrameworkCore`.

Behavior that depends on the database — case sensitivity, collation, null ordering,
and string comparison — is documented as provider-dependent rather than promised
uniformly.

## Pagination is a value, not a side effect

`PagedResult<T>` carries both the page items and the metadata:

- `Page`, `PageSize`, and `TotalPages` describe where the caller is.
- `TotalCount` describes the size of the full result set.
- `HasPreviousPage` and `HasNextPage` avoid callers recomputing the same conditions.

The same type is returned by the synchronous and asynchronous operations, so an
endpoint can switch execution strategies without changing its contract.

## Explicit ordering

Paginating without a deterministic `ORDER BY` is undefined at the provider level.
Rows may be duplicated or skipped between pages. Laranet documents this requirement
but does not silently add an ordering, because only the application knows which key is
stable and meaningful.

Order by a unique key when possible:

```csharp
var page = db.Products
    .OrderBy(product => product.Id)
    .Paginate(request.Page, request.PageSize);
```

## Conditional construction

`WhereIf` exists because many endpoints receive optional filters. It keeps the query
readable without branching around the whole chain:

```csharp
var query = db.Products
    .WhereIf(request.MinPrice.HasValue,
        product => product.Price >= request.MinPrice!.Value)
    .WhereIf(request.OnlyActive == true,
        product => product.IsActive);
```

The predicate is always evaluated as an expression. The condition only decides
whether it is included in the final query.

## Escape hatches

Laranet never hides the underlying platform:

- Any extension returns a normal `IQueryable<T>`, so native LINQ operators still work.
- `PagedResult<T>` is a plain class, so it can be serialized or mapped normally.
- Generated code and integrations use native EF Core and ASP.NET Core APIs.
- A package can be removed without changing the application's data model.

The rule is: **simplify the common path, never block the native API.**

## Optional modules

Each package is independently consumable. Installing `Laranet.Data` does not install
EF Core or ASP.NET Core. Installing an integration does not force the CLI or the core
package.

The [ecosystem architecture](../architecture/ecosystem.md) defines the dependency
rules and the conditions a new module must satisfy.

## Related

- [Project philosophy](philosophy.md)
- [Dependency injection](dependency-injection.md)
- [Extensibility](extensibility.md)
- [Design principles](../architecture/design-principles.md)
