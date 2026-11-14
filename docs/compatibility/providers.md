# Provider Compatibility

## Purpose

This document describes the current provider compatibility of `Laranet.Data` and the
EF Core integration, and the expectations for `IQueryable` providers. The matrix
applies to the current `0.1.x` release line.

`Laranet.Data` is provider-neutral. It builds standard `Queryable` expression trees
and does not reference Entity Framework Core, ASP.NET Core, or any database provider.
Translation and execution are always delegated to the underlying provider.

## Support Matrix

| Provider                                   | Status               | Verified by                     |
| ------------------------------------------ | -------------------- | ------------------------------- |
| SQLite via Entity Framework Core           | Verified             | Integration and provider compatibility test suites |
| SQL Server via Entity Framework Core       | Verified             | Provider compatibility tests and CI service container |
| PostgreSQL via Entity Framework Core       | Verified             | Provider compatibility tests and CI service container |
| MySQL 8.4 via Entity Framework Core        | Verified             | Provider compatibility tests and CI service container |
| MariaDB 11.4 via Entity Framework Core     | Verified             | Provider compatibility tests and CI service container |
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
- Multi-column `Search`, translated into a single `OR` expression.
- Typed and dynamic sorting, including secondary ordering.
- Dynamic sorting allowlists reject disallowed names before any command executes.
- `WhereIf` adds or omits a filter based on its condition.
- `Paginate` executes the expected count and page queries.
- Out-of-range pages and empty collections execute only the count query.
- Pagination metadata remains correct in all covered cases.
- Composition between search, filtering, sorting, and pagination.
- Asynchronous pagination through `Laranet.EntityFrameworkCore`, including command
  execution, metadata, cancellation, and composition.
- Created and updated timestamp auditing through `IAuditable`.
- Soft delete through `ISoftDeletable`, including `DeletedAt`, `IgnoreQueryFilters()`,
  and `UpdatedAt` for auditable entities.
- Composition between the soft-delete query filter and an existing query filter.

The provider compatibility suite runs against the provider selected by
`LARANET_TEST_PROVIDER` and covers:

- Single-column and multi-column `Search`.
- Typed and dynamic sorting, including allowlists.
- `WhereIf`.
- Synchronous and asynchronous pagination.
- Composition between search, filtering, sorting, and pagination.
- Parameterization of the search term.
- Auditing and soft-delete conventions, including timestamp round trips.

SQLite runs in the default test job. SQL Server, PostgreSQL, MySQL, and MariaDB run
against CI service containers on every push and pull request to `main`. The provider
suite also passed locally against SQL Server 2022, PostgreSQL 17, MySQL 8.4, and
MariaDB 11.4.

## Verified Provider Differences

The same `Search` call is translated differently by the verified providers. For
example:

| Provider | Translation of `string.Contains` |
| --- | --- |
| SQLite | `instr(column, @term)` |
| SQL Server | `LIKE @Value_contains ESCAPE N'\'` with a `%term%` parameter |
| PostgreSQL | `LIKE @Value_contains` with a `%term%` parameter |

These are provider implementation details. `Laranet.Data` composes the expression and
does not embed provider-specific SQL. The compatibility tests assert results and
parameterization rather than exact SQL text.

The auditing and soft-delete conventions also expose a provider difference around
`DateTimeOffset`:

| Provider | Storage | Offset behavior |
| --- | --- | --- |
| SQLite | `TEXT` | Preserved. |
| SQL Server | `datetimeoffset` | Preserved. |
| PostgreSQL | `timestamp with time zone` | Only a zero (UTC) offset is accepted. |
| MySQL 8.4 and MariaDB 11.4 through `MySql.EntityFrameworkCore` | `datetime(6)` | Normalized to UTC; the original offset is lost. |

The interceptors use `TimeProvider.GetUtcNow()`, so the default convention produces
UTC timestamps that every verified provider accepts. The instant is preserved in all
cases; only the offset can change or be rejected. A custom `TimeProvider` must return
UTC when the application targets PostgreSQL. The provider compatibility suite asserts
UTC round trips and the offset rule for each verified provider.

## Entity Framework Core Integration

`Laranet.EntityFrameworkCore` adds asynchronous execution, auditing, and soft delete on
top of `Laranet.Data`. It is verified with SQLite, SQL Server, PostgreSQL, MySQL, and
MariaDB through Entity Framework Core. Other EF Core providers inherit the same status
as their provider row above: they are expected to work, but they are not verified by
the current test matrix.

The package depends on `Microsoft.EntityFrameworkCore` 10.x. The synchronous behavior
of `Laranet.Data` remains provider-neutral and does not reference EF Core. The
conventions do not add a database provider dependency to the production package.

## Running provider compatibility tests

The provider is selected with an environment variable:

```bash
LARANET_TEST_PROVIDER=sqlite dotnet test Laranet.slnx --configuration Release
LARANET_TEST_PROVIDER=sqlserver dotnet test Laranet.slnx --configuration Release \
  --filter "FullyQualifiedName~ProviderCompatibilityTests"
LARANET_TEST_PROVIDER=postgresql dotnet test Laranet.slnx --configuration Release \
  --filter "FullyQualifiedName~ProviderCompatibilityTests"
LARANET_TEST_PROVIDER=mysql dotnet test Laranet.slnx --configuration Release \
  --filter "FullyQualifiedName~ProviderCompatibilityTests"
LARANET_TEST_PROVIDER=mariadb dotnet test Laranet.slnx --configuration Release \
  --filter "FullyQualifiedName~ProviderCompatibilityTests"
```

`LARANET_SQLSERVER_CONNECTION`, `LARANET_POSTGRES_CONNECTION`, and
`LARANET_MYSQL_CONNECTION` override the default local connection strings. The
`mysql` and `mariadb` values use the same `MySql.EntityFrameworkCore` provider. CI
provides the service containers and connection strings.

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
- `DateTimeOffset` storage and offset handling, as described above.
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
- The tests cover search, sorting, filtering, pagination, and the opt-in conventions.
- The tests run in a repeatable environment.

Coverage claims in this document must match what the test suite actually executes.
