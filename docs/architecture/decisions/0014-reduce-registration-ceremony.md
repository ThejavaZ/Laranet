# ADR-0014: Reduce Registration Ceremony with Declarative Options

- **Status:** Accepted — prototype implemented, pending real-application validation
- **Date:** 2026-11-16
- **Related:** ADR-0006 (toolkit with framework-like tooling), ADR-0012 (admin
  provider-neutral core), ADR-0013 (admin architecture and security boundary).

## Context

Registering the administrative surface currently requires repeated imperative
code. A real application with two resources (Products and Brands) needed roughly
28 lines of registration ceremony in `Program.cs`:

- a factory lambda for `IAdminResourceRegistry`,
- a factory lambda for `IAdminDataAdapter`,
- one base authorization policy per resource,
- a loop over `AdminOperation` registering six per-operation policies per
  resource.

The cost grows linearly with every new resource, and the sample teaches the
per-operation loop even though `AspNetCoreAdminAuthorization` already falls back
to the resource's base policy when `Resource.Operation` is not registered. The
loop is redundant for the common case.

Developer experience is the first project priority. Developers expect
declaration-style integration points such as `app.MapOpenApi()`: declare once and
derive the rest from metadata that already exists. Django Admin reaches this
level through `admin.site.register(Model)` plus model introspection — a single
source of truth consumed by the UI.

The constraint is ADR-0006 and ADR-0013: the runtime stays explicit,
provider-neutral, and composed from native services. Convenience may remove
repetition, but it must not hide the platform.

Auto-discovery exists (`AdminResourceAttribute`, `AdminResourceDiscovery`) but is
exposed as a multi-line factory lambda, and its type filter matches no supported
resource shape (neither typed `AdminResource<T>` nor static `Definition` classes
derive from the sealed `AdminResource` record), so discovery currently finds
nothing.

## Decision

Registration moves to a single declarative options callback while the
registrations underneath remain plain DI:

1. `AddLaranetAdmin(Action<AdminRegistrationOptions>)` becomes the preferred
   entry point. The options object is a provider-neutral data bag:
   - `AddResource(...)` accepts an explicit `AdminResource` definition or a
     resource class exposing `Definition` (typed `AdminResource<T>` or static).
   - `DiscoverResources(assembly)` registers every class marked with
     `AdminResourceAttribute` that exposes a `Definition` property.
   - `AddAdapter(factory)` records the `IAdminDataAdapter` factory.
2. `Laranet.Admin.EntityFrameworkCore` contributes `UseEfCore<TContext>()` as an
   extension method over the options, preserving package dependency boundaries
   (the core never references EF Core or ASP.NET Core).
3. Base policies named by each resource's `PermissionPolicy` are registered only
   when absent, when `AuthorizationOptions` is materialized, defaulting to
   `RequireAuthenticatedUser`. Explicit application policies always win,
   regardless of registration order.
4. The sample and guide stop registering per-operation policies. The fallback in
   `AspNetCoreAdminAuthorization` becomes documented behavior;
   `Resource.Operation` policies remain an explicit opt-in for applications that
   need different requirements per operation.
5. Discovery accepts any class exposing a `Definition` property, fixing the type
   filter that previously matched no known resource shape.

### Future directions (not implemented here)

- Field inference from public properties and DataAnnotations (Django-style),
  with explicit declarations always winning over inferred ones.
- A synthesized `AuthorizationPolicyProvider` for on-demand per-operation
  policies.

Both directions require their own ADR when implemented.

## Alternatives considered

- **Status quo** — rejected: registration cost grows linearly per resource and
  the sample documents a redundant pattern.
- **Full magic (`app.MapAdmin()` hiding all services)** — rejected: it hides
  ASP.NET Core composition and conflicts with ADR-0006 and ADR-0013.
- **Attribute-only registration** — rejected: attributes cannot express adapter
  or provider wiring and would scatter configuration beyond `Program.cs`.

## Consequences

Positive:

- The whole admin surface registers in three to six lines; discovered resources
  add no `Program.cs` lines at all.
- One fluent vocabulary, consistent with the existing
  `AddLaranetAdminAuthentication(options => ...)` pattern.
- Discovery works for both supported resource shapes and is covered by tests.
- The per-operation loop disappears from samples and applications.

Negative:

- Another experimental options surface to keep stable until a real application
  validates it.
- Add-if-absent policy behavior must be documented so applications know that
  explicit policies should be registered through the same callback or afterwards.
- The future field-inference direction carries convention risk and must stay
  overridable by explicit declarations.
