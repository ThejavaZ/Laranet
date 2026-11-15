# ADR-0012: Keep the Admin Core Provider-Neutral

- **Status:** Accepted for prototype
- **Date:** 2026-11-14

## Context

Laranet needs a useful administrative experience without becoming an application
framework. A Django-inspired panel is valuable only if it works with native ASP.NET
Core, Razor Pages, authentication, and more than one data technology.

The first validated use case is a product catalogue managed through Razor Pages,
SQLite, and ASP.NET Core Identity. The prototype must validate list, search, filter,
pagination, create, edit, and delete flows without allowing the first EF Core sample
to define the core package.

## Decision

The administrative surface is split into two responsibilities:

- `Laranet.Admin` owns provider-neutral metadata and contracts.
- A future `Laranet.Admin.AspNetCore` integration may own Razor Pages conventions and
  ASP.NET Core integration after the prototype is validated.

The current prototype is deliberately an application sample. It uses
`Laranet.Admin` contracts with a local EF Core adapter and native Razor Pages instead
of prematurely freezing a UI package.

### Core contracts

`Laranet.Admin` contains:

- `AdminResource` with fields, filters, actions, and a permission policy name.
- `AdminQuery` and `AdminPage` for search, filtering, and pagination.
- `IAdminDataAdapter` for list, read, create, update, and delete operations.
- `IAdminAuthorization` for permission decisions.
- `IAdminResourceRegistry` for explicit resource registration.

The core package does not reference EF Core, ASP.NET Core, Razor, Identity, or a
database provider.

### Data adapter

The adapter owns query composition, filtering, pagination, mapping, persistence, and
provider-specific behavior. The core never receives `IQueryable`, `DbContext`, or
provider types.

The prototype adapter supports the `Product` resource over SQLite. A future Dapper,
PostgreSQL, or other adapter can implement the same contract without changing core
metadata.

### Security

Authentication and authorization remain native ASP.NET Core responsibilities:

- The prototype uses ASP.NET Core Identity.
- Razor Pages use an explicit `Admin.Products` authorization policy.
- `IAdminAuthorization` is a permission hook, not an authentication system.
- Laranet does not create users, passwords, roles, cookies, tokens, or a
  `Laranet.Security` package.
- Mutating forms use Razor's native antiforgery support.

### UI

The prototype uses ordinary Razor Pages. No custom view engine, component system, or
server-side rendering abstraction is introduced. A future
`Laranet.Admin.AspNetCore` package must earn its boundary through repeated, validated
conventions.

## Consequences

### Positive

- The product use case is executable and testable today.
- EF Core remains an adapter concern.
- Users can use native Razor Pages and Identity escape hatches.
- The core API is small enough to replace or revise before a stable release.

### Negative

- The first prototype contains application-specific mapping code.
- The initial metadata model is not sufficient for every form or relationship.
- A future UI package may need a breaking revision after real feedback.
- No automatic CRUD generation or convention discovery is provided.

## Out of scope

- A custom ORM, query language, or repository/unit-of-work abstraction.
- Authentication, authorization, roles, users, or audit infrastructure.
- A custom view engine or JavaScript component library.
- Automatic resource discovery.
- EF Core dependencies in `Laranet.Admin`.
- Declaring the prototype API stable or publishing an admin UI package.

## Validation

The prototype is validated with:

- SQLite adapter integration tests for list/search/pagination and CRUD.
- Resource registry tests for registration and key-field invariants.
- A buildable Razor Pages sample using Identity and a protected Products area.

Before promoting the contracts to a stable package, validate the same flows against a
real application and review filtering, authorization, anti-forgery, and provider
behavior.

## Related

- [ADR-0002: Use Optional Framework Integrations](0002-optional-integrations.md)
- [ADR-0006: Position Laranet as a Modular Toolkit with Framework-like Tooling](0006-toolkit-with-framework-tooling.md)
- [Version 4 roadmap](../../roadmap/v4.md)
- [Version 3 roadmap](../../roadmap/v3.md)
