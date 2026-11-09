# ADR-0005: Keep the Core Package Intentionally Empty

- **Status:** Accepted
- **Date:** 2026-11-08

## Context and Problem Statement

The `Laranet` package is published as `0.1.0` and currently exposes no public API.
The other modules — `Laranet.Data`, `Laranet.EntityFrameworkCore`,
`Laranet.AspNetCore`, and `Laranet.Cli` — do not reference it.

This raises a legitimate question: should the package that carries the name of the
project contain the shared contracts of the ecosystem?

Several proposals have been made:

- Move `PagedResult<T>` and `SortDirection` into the core package.
- Introduce a transversal `Result<T>` / `Error` pattern.
- Add infrastructure abstractions such as `IClock`.
- Keep the package empty until a shared abstraction is actually required.

Filling the package without a demonstrated need would create a compatibility surface
and a dependency direction before the project knows what it needs.

## Decision Drivers

- Preserve the dependency direction established by ADR-0001 and ADR-0002.
- Keep `Laranet.Data` free of dependencies on other Laranet packages.
- Avoid speculative abstractions and the compatibility cost they create.
- Give users a stable, predictable package identity.
- Make room for real shared abstractions when they appear.
- Keep the toolkit aligned with native .NET conventions.

## Considered Options

- Fill the package with speculative contracts such as `Result`, `Error`, and `IClock`.
- Move existing types (`PagedResult<T>`, `SortDirection`) into the core package.
- Publish the package as a meta-package that references the other modules.
- Keep the package intentionally empty until a shared abstraction is required.

## Decision Outcome

Chosen option: **Keep the `Laranet` package intentionally empty until an abstraction
is required by at least two modules.**

The following rules apply:

- A type moves into `Laranet` only when at least two published packages need it and
  neither package can depend on the other.
- Moving a type into the core package requires an ADR update, a compatibility plan,
  and a versioning decision.
- `Laranet.Data` remains independent of `Laranet` as long as it has no shared
  abstraction to consume. Its provider neutrality is a feature, not an accident.
- `Result<T>`, `Error`, Repository, Unit of Work, and similar patterns are not added
  without a concrete use case and an explicit architectural decision.
- `Laranet.Cli` remains a .NET tool and does not gain a runtime API.
- The package metadata and README explain that the package is intentionally empty in
  the `0.1.x` line.

This decision does not prevent the core package from gaining functionality later. It
defines the threshold that any addition must meet.

## Consequences

### Positive Consequences

- The package identity is reserved without promising behavior that does not exist.
- No compatibility obligation is created for speculative types.
- The dependency graph stays simple and predictable.
- `Laranet.Data` keeps its zero-dependency status.
- Future shared abstractions must justify themselves with a real use case.

### Negative Consequences

- Users may perceive the package as incomplete.
- The ecosystem cannot share types through `Laranet` yet.
- Any future move of existing types will be a breaking change and will require
  migration guidance.
- The package must be explained clearly in its README and documentation.

## Confirmation

This decision is validated by:

- The published `0.1.0` package having no public API.
- Documentation stating that the package is intentionally empty.
- Architecture reviews confirming that no module depends on `Laranet` without a
  justified shared abstraction.
- A new ADR before any type moves into the package.

## Related Decisions

- [ADR-0001: Keep the Core Independent](0001-independent-core.md)
- [ADR-0002: Use Optional Framework Integrations](0002-optional-integrations.md)
- [ADR-0003: Maintain Independent Project Versioning](0003-independent-versioning.md)
- [ADR-0004: Generate Ordinary, Editable C#](0004-editable-generated-code.md)
