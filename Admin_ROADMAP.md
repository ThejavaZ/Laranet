# Admin Roadmap — Developer Experience

This roadmap tracks the work to make the admin panel the simplest possible
integration for developers: declare metadata once and let the toolkit derive
the rest. The reference points are `app.MapOpenApi()` (declare and done) and
Django's `admin.site.register(Model)` (the framework introspects model
metadata).

The decisions behind this roadmap live in
[ADR-0014](docs/architecture/decisions/0014-reduce-registration-ceremony.md).
UI component work is tracked separately in
[ADMIN_UI_BACKLOG](docs/ADMIN_UI_BACKLOG.md).

## North star

The target experience once the phases below land:

```csharp
builder.Services.AddLaranetAdmin(options => options
    .DiscoverResources(typeof(Program).Assembly)
    .UseEfCore<AppDbContext>());

app.MapRazorPages();
```

A handful of lines for the whole panel: resources, fields, filters, actions,
and policies derived from metadata that already exists.

## Shipped

- **P0 — Remove redundant per-operation policy loops.** The authorization bridge
  falls back to the resource's base policy, so the sample and guide no longer
  teach the loop. Per-operation policies remain an explicit opt-in.
- **P1 — Declarative registration.** `AddLaranetAdmin(options => ...)` with
  `AddResource(...)` (explicit definition or typed class), `AddAdapter(...)`,
  and default base policies registered only when absent — explicit application
  policies always win, in any registration order.
- **P2 — One-line discovery.** `DiscoverResources(assembly)` works for both
  resource shapes (typed `AdminResource<T>` and static `Definition` classes) and
  honors `AdminResourceAttribute` metadata (`Name`, `PermissionPolicy`).

Shipped with ADR-0014, sample migration, guide updates, and tests.

- **P3 — Field inference from model metadata.** `AdminResource<TModel>` seeds
  the field set from public properties and DataAnnotations (`[Key]`,
  `[Required]`, `[Display]`, `[Range]`, `[Editable]`, `[ScaffoldColumn]`);
  `Field()` merges so explicit declarations always win; and
  `AddResourceFromModel<TModel>("Admin.Policy", ...)` builds a complete resource
  from the model alone — no resource class required. Shipped with ADR-0015,
  sample migration, guide updates, and tests.

- **P4 — Synthesized operation policies.** An `IAuthorizationPolicyProvider`
  materializes `Resource.Operation` policies from the registered base policy on
  demand: `[Authorize(Policy = "Admin.Products.Delete")]` works with zero
  registration, and an explicit operation policy still overrides the
  synthesized one. Shipped with ADR-0016.

## Later — polish

- Relation and section conventions inferred from the model (`BelongsTo`
  candidates, form grouping).
- Dashboard cards inferred from metadata.
- Every phase lands with tests, sample, and guide updates in the same change.

## Rules for every phase

1. Native .NET first: DataAnnotations, reflection, and standard ASP.NET
   patterns — no custom abstraction where the platform has an answer.
2. Explicit beats inferred: anything inferred can be overridden in
   `Configure()` or by an explicit registration.
3. No hidden runtime magic: metadata is resolved and cached at startup.
4. Provider-neutral core: sugar belongs in the integration packages
   (`Laranet.Admin.AspNetCore`, `Laranet.Admin.EntityFrameworkCore`).
5. Ship with tests, sample, and guide updates in the same change.

## Success metric

`Program.cs` cost per new resource:

| State | Cost |
| --- | --- |
| Before ADR-0014 | ~9 lines (registry, policies, operation loop) |
| After P1/P2 | 0 lines with discovery, 1 line explicit |
| After P3 | resource class shrinks to overrides only |
