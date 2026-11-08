# Provider Compatibility

## Purpose

This document describes the current provider compatibility of `Laranet.Data` and the
expectations for `IQueryable` providers.

`Laranet.Data` is provider-neutral. It builds standard `Queryable` expression trees
and does not reference Entity Framework Core, ASP.NET Core, or any database provider.
Translation and execution are always delegated to the underlying provider.

## Support Matrix

| Provider                                   | Status               | Verified by                     |
| ------------------------------------------ | -------------------- | ------------------------------- |
| SQLite via Entity Framework Core           | Verified             | Integration test suite          |
| SQL Server via Entity Framework Core       | Expected, not verified | No automated coverage         |
| PostgreSQL via Entity Framework Core       | Expected, not verified | No automated coverage         |
| Other EF Core relational providers         | Expected, not verified | No automated coverage         |
| `EnumerableQuery<T>` (LINQ to Objects)     | Works for query semantics | Unit test suite            |
| Other non-EF `IQueryable` providers        | Not verified         | No automated coverage           |

"Expected, not verified" means the operations are built from standard query
operators and should translate, but the project has not validated them against the
provider. Do not treat this status as a compatibility guarantee.

## What Is Verified

The integration test suite runs against SQLite through Entity Framework Core and
verifies:

- `Search` translation and parameterization of the search term.
- Typed and dynamic sorting, including secondary ordering.
- Dynamic sorting allowlists reject disallowed names before any command executes.
- `WhereIf` adds or omits a filter based on its condition.
- `Paginate` executes the expected count and page queries.
- Out-of-range pages and empty collections execute only the count query.
- Pagination metadata remains correct in all covered cases.
- Composition between search, filtering, sorting, and pagination.

## Provider Requirements

A provider can be expected to work when it:

- Implements `IQueryable<T>` and standard `Queryable` operators.
- Translates the expressions produced by the extensions.
- Supports the operators used by the requested operation, such as `Where`, `OrderBy`,
  `ThenBy`, `Skip`, and `Take`.
- Provides `string.Contains` translation for `Search` (directly or through its
  client-side translation capabilities).

Providers that execute queries in memory can still use the extensions for query
composition, but no SQL translation is involved.

## Provider-Specific Behavior

The following behaviors depend on the provider and database, not on `Laranet.Data`:

- String comparison, case sensitivity, and collation used by `Search`.
- Null ordering and collation used by sorting.
- Translation of computed or unmapped properties.
- Pagination stability when the query has no deterministic ordering.

Order the query before paginating, preferably by a unique key.

## Asynchronous Execution

`Paginate` is synchronous. Asynchronous execution belongs to provider-specific
integrations, which may add extension methods without changing the behavior of
`Laranet.Data`.

## Adding Provider Coverage

Provider support should be promoted from "expected" to "verified" only when:

- Integration tests run against the provider.
- The tests cover search, sorting, filtering, and pagination translation.
- The tests run in a repeatable environment.

Coverage claims in this document must match what the test suite actually executes.
