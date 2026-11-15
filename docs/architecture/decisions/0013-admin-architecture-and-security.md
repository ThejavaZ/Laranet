# ADR-0013: Define the Admin Architecture and Security Boundary

- **Status:** Accepted for implementation, real-application validation pending
- **Date:** 2026-11-15
- **Supersedes:** The prototype decisions in ADR-0012 where this contract is more
  specific.

## Context

The Product/SQLite/Razor Pages prototype validated that Laranet can provide a useful
administrative experience without replacing ASP.NET Core, EF Core, Razor, or Identity.
The next step is to define the boundary before implementing a reusable UI.

The desired experience is Django-like in productivity, not Django-like in runtime
architecture: users register resources and receive conventional list, search, filter,
pagination, and CRUD screens, while native .NET escape hatches remain available.

## Decision

The administrative surface has three conceptual layers:

```text
Laranet.Admin
    provider-neutral metadata, query/page contracts, adapter and permission hooks

Laranet.Admin.AspNetCore
    optional Razor Pages integration, routes, authorization and antiforgery

Application or future adapter package
    EF Core, Dapper, ADO.NET, mapping, persistence and provider behavior
```

The UI package is not implemented or published by this ADR. Sprint 23 may implement
the core contract first, and Sprint 24 may implement the Razor integration after the
contract has been reviewed against a real application.

### Resource registration

Resources are registered explicitly. There is no assembly-wide automatic discovery.
A resource describes:

- Identity and model type.
- Display fields and key field.
- Searchable fields.
- Supported filters and ordering.
- Mutating and non-mutating actions.
- A permission policy or permission hook.

The prototype's `AdminResource`, `AdminField`, `AdminFilter`, `AdminAction`, and
`IAdminResourceRegistry` are the current experimental vocabulary. Sprint 23 may
replace their exact shape, but it must preserve these responsibilities.

### Data adapter contract

The adapter is responsible for:

- List with search, filters, ordering, and pagination.
- Read by key.
- Create.
- Update.
- Delete or the resource's configured delete action.

The adapter owns query composition, mapping, validation that depends on persistence,
transactions, concurrency, and provider-specific behavior. The core contract must
not expose `IQueryable`, `DbContext`, SQL, or a provider type.

The adapter returns a provider-neutral page and row representation to the UI layer.
An adapter may reject unsupported filters or actions explicitly; it must not silently
ignore them.

No repository, unit-of-work, ORM, or `Result<T>` abstraction is introduced.

### ASP.NET Core and Razor integration

`Laranet.Admin.AspNetCore` will be optional and use ordinary Razor Pages:

- Conventional routes begin under `/admin`.
- A resource route uses `/admin/{resource}`.
- Create, edit, detail, and delete actions use conventional child routes.
- Applications can replace pages, layouts, authorization policies, and handlers with
  native Razor mechanisms.
- The integration does not create a view engine, component framework, or JavaScript
  runtime.

The package may depend on `Laranet.Admin` and ASP.NET Core. It must not make
`Laranet.AspNetCore`, EF Core, Identity, or a database provider mandatory.

### Security model

Authentication remains the application's responsibility. The Admin integration:

- Uses native ASP.NET Core authorization policies.
- Requires authorization before listing a resource or executing an action.
- Allows resource and action permission hooks.
- Uses native antiforgery protection for every mutating Razor request.
- Does not create users, roles, cookies, tokens, password storage, or audit storage.
- Does not introduce `Laranet.Security`.

The prototype's `Admin.Products` policy is the reference use case. A default policy
may be conventional, but applications must be able to override it.

### Validation and errors

Resource metadata can provide display and validation hints, but validation remains
native:

- DataAnnotations for simple form rules.
- FluentValidation through the existing optional integration where appropriate.
- Domain/application validation for rules requiring business logic or external state.

The integration returns native validation problem behavior for HTTP requests and
normal Razor model-state behavior for pages. It does not introduce a custom result
or exception hierarchy.

## Consequences

### Positive

- The Django-like experience comes from explicit conventions, not hidden framework
  behavior.
- EF Core remains replaceable and absent from the core package.
- Identity and authorization remain native and auditable.
- The UI can be adopted incrementally or replaced with application Razor Pages.
- The current Product prototype remains a useful validation fixture.

### Negative

- Resource registration and adapter mapping require explicit application code.
- The generic UI cannot know every domain rule or relationship.
- The exact metadata API may change during Sprint 23.
- A real application is still required before publishing the UI package.

## Explicit non-goals

- Automatic discovery of every entity or DbContext.
- A generic repository or unit of work.
- A custom ORM, query language, or persistence transaction manager.
- Authentication, authorization, roles, or audit infrastructure.
- A custom view engine or frontend framework.
- Bulk operations, dashboards, imports, relationship editors, and workflows in the
  first vertical slice.
- Publishing `Laranet.Admin.AspNetCore` before real-application validation.

## Validation gates

Before Sprint 24 publishes or stabilizes the UI contract:

1. Review the resource and adapter contract against a real application.
2. Test list, search, filter, pagination, read, create, update, and delete.
3. Test authorization denial for resource and mutating actions.
4. Test antiforgery on every mutating request.
5. Verify that `Laranet.Admin` has no EF Core or ASP.NET Core dependency.
6. Verify an application can replace the default Razor page or adapter.

## Related

- [ADR-0012: Keep the Admin Core Provider-Neutral](0012-admin-boundary-and-prototype.md)
- [ADR-0002: Use Optional Framework Integrations](0002-optional-integrations.md)
- [Version 4 roadmap](../../roadmap/v4.md)
- [Admin prototype guide](../../guides/admin.md)
