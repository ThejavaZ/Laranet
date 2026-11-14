# ADR-0009: Keep EF Core Conventions Optional and Close to the Framework

- **Status:** Accepted
- **Date:** 2026-11-12

## Context and Problem Statement

The Version 3 roadmap asks Laranet to evaluate declarative conventions on top of
Entity Framework Core: entity and primary-key conventions, `CreatedAt` / `UpdatedAt`
auditing, soft delete with a global query filter, and general global filters.

Entity Framework Core already provides conventions for entity discovery, primary keys,
relationships, and global query filters. Adding a parallel convention engine would
duplicate framework behavior, hide configuration from the `DbContext`, and create a
compatibility surface that Laranet must maintain across providers.

At the same time, two common application concerns are repetitive to write by hand:

- Stamping creation and modification timestamps.
- Converting physical deletes into a flag update and excluding deleted rows from
  queries.

The project needs to decide which conventions Laranet should offer, where they belong,
and how users opt out.

## Decision Drivers

- Keep the `DbContext` as the center of persistence and configuration.
- Prefer native EF Core behavior over parallel abstractions.
- Keep every convention optional and independently adoptable.
- Avoid provider-specific APIs in a provider-neutral integration package.
- Keep public APIs small, predictable, and testable.
- Use `TimeProvider` so timestamps are testable.
- Preserve `IgnoreQueryFilters()` as the native escape hatch.

## Considered Options

### Option 1 — Build a full convention engine

Add attributes or fluent helpers for keys, table names, relationships, auditing, soft
delete, and global filters, and apply them through a custom metadata pipeline.

- Advantage: a single, Laravel-like configuration surface.
- Risk: duplicates EF Core conventions, hides native configuration, and grows a large
  public API with little demonstrated value.

### Option 2 — Add no conventions

Document native EF Core behavior and leave auditing and soft delete to every
application.

- Advantage: the smallest possible package and no new maintenance surface.
- Risk: the same error-prone interception code is rewritten in every Laranet
  application, with inconsistent timestamp and delete behavior.

### Option 3 — Add opt-in conventions that delegate to EF Core

Do not abstract entity discovery, primary keys, or general query filters. Provide only
marker interfaces and `SaveChanges` interceptors for the two concerns EF Core does not
cover by convention:

- Auditing through `IAuditable`.
- Soft delete through `ISoftDeletable` and a model-building helper that installs a
  native query filter.

- Advantage: small, explicit, provider-neutral, and close to the framework.
- Risk: users still write the three interface properties, and the helpers must be
  registered in both `OnConfiguring` and `OnModelCreating`.

## Decision Outcome

Chosen option: **Option 3 — add opt-in conventions that delegate to EF Core.**

### What Laranet will provide

In `Laranet.EntityFrameworkCore`:

- `IAuditable` with settable `CreatedAt` and `UpdatedAt` properties.
- `ISoftDeletable` with settable `IsDeleted` and `DeletedAt` properties.
- `AddLaranetAuditing(TimeProvider?)` on `DbContextOptionsBuilder`, which registers a
  `SaveChanges` interceptor that stamps auditable entities.
- `AddLaranetSoftDelete(TimeProvider?)` on `DbContextOptionsBuilder`, which registers
  an interceptor that converts `Deleted` entries into `Modified` entries and stamps
  `IsDeleted` and `DeletedAt`.
- `ApplySoftDeleteQueryFilters()` on `ModelBuilder`, which installs native
  `HasQueryFilter` expressions for root entity types that implement `ISoftDeletable`.

### What Laranet will not provide

- A key, table, or entity-name convention API. EF Core already discovers `Id`,
  `<Type>Id`, and CLR navigation properties.
- A general global-filter wrapper. Applications call `HasQueryFilter` directly.
- `HasMany<T>` or `BelongsTo<T>` relationship wrappers.
- A repository, unit of work, or base `DbContext`.
- User identifiers (`CreatedBy` / `UpdatedBy`) in this decision. They require request
  identity integration and belong to a later, separately justified decision.

### Rules

1. Every convention is opt-in. Referencing the package and using a `DbContext` without
   calling the helpers changes no behavior.
2. Auditing only affects entities that implement `IAuditable`.
3. Soft delete only affects entities that implement `ISoftDeletable`.
4. `ApplySoftDeleteQueryFilters()` combines its condition with an existing anonymous
   filter and adds a named filter when the entity type already uses named filters. It
   never replaces a user-defined filter.
5. `IgnoreQueryFilters()` remains the supported way to include soft-deleted rows.
6. `DeletedAt` and `UpdatedAt` are nullable; a newly added auditable entity has
   `CreatedAt` set and `UpdatedAt` unset.
7. Soft-deleting an auditable entity also updates `UpdatedAt`.
8. The helpers use `TimeProvider.System` unless a different `TimeProvider` is supplied.
9. The conventions run in `SaveChanges` and `SaveChangesAsync`; they do not alter
   migrations or the database schema beyond the mapped properties.
10. Provider-specific timestamp storage is documented as provider behavior, not hidden
    by the integration package.

## Consequences

### Positive Consequences

- Applications can adopt auditing and soft delete with two explicit registration calls.
- The public API stays small and easy to test.
- Native EF Core conventions, filters, and `IgnoreQueryFilters()` remain authoritative.
- The integration package stays provider-neutral.

### Negative Consequences

- Users must map the interface properties in their entities and confirm the columns in
  migrations.
- The soft-delete query filter is installed explicitly; forgetting the model helper
  causes deleted rows to remain visible.
- The query filter helper must document its behavior with existing filters, owned types,
  and inheritance.
- `DateTimeOffset` offset preservation depends on the provider and must be tested.

## Confirmation

This decision is validated by:

- SQLite integration tests covering timestamps, soft delete, query filters,
  `IgnoreQueryFilters()`, restoration, and custom filter composition.
- Provider compatibility tests against SQLite, SQL Server, PostgreSQL, MySQL, and
  MariaDB.
- Package documentation, the relationships guide, and the provider compatibility
  matrix describing the opt-in boundary and provider differences.

## Out of Scope

- User-stamped auditing through ASP.NET Core identity.
- Multi-tenancy and tenant filters.
- Cascading soft delete.
- Hard-delete archives or audit tables.
- Relationship wrappers and key conventions.

## Related Decisions

- [ADR-0002: Use Optional Framework Integrations](0002-optional-integrations.md)
- [ADR-0006: Position Laranet as a Modular Toolkit with Framework-like Tooling](0006-toolkit-with-framework-tooling.md)
- [ADR-0008: Keep Runtime and Tooling Packages on One Version Train by Default](0008-runtime-and-tooling-versioning.md)
- [Version 3 roadmap](../../roadmap/v3.md)
