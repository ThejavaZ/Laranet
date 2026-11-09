# ADR-0006: Position Laranet as a Modular Toolkit with Framework-like Tooling

- **Status:** Accepted
- **Date:** 2026-11-08

## Context and Problem Statement

Laranet started as a Laravel-inspired toolkit for .NET. As the CLI and the number of
proposed modules grow, the project needs a clear identity:

- Is it a library of independent utilities?
- Is it an opinionated application framework with its own host and lifecycle?
- Is it something in between?

The answer affects package boundaries, public APIs, documentation, and the promises
made to users. It also determines whether Laranet competes with ASP.NET Core and EF
Core or complements them.

## Decision Drivers

- Preserve the modularity established by ADR-0001 and ADR-0002.
- Offer a coherent developer experience without hiding native .NET capabilities.
- Avoid maintaining a second web framework, ORM, DI container, or view engine.
- Keep every package independently consumable.
- Make the project understandable to new contributors and users.
- Preserve room to evolve without locking the identity prematurely.

## Considered Options

- A library of independent utilities with no shared conventions.
- A full opinionated framework with its own host, lifecycle, and mandatory structure.
- A modular toolkit with framework-like tooling: conventions, templates, generators,
  and optional integrations on top of native .NET.

## Decision Outcome

Chosen option: **A modular toolkit with framework-like tooling.**

Laranet:

- Does not define an application host, startup lifecycle, or mandatory base classes.
- Does not replace ASP.NET Core, EF Core, dependency injection, configuration, or
  logging.
- Provides conventions, project templates, code generation, and optional integrations
  that make native .NET more productive.
- Keeps every module optional and independently consumable.
- Allows generated code to be edited, replaced, or abandoned without losing
  functionality.
- Exposes an escape hatch: users can always use the underlying .NET APIs directly.

The word "framework" is reserved for a future decision. Laranet may offer a
framework-like experience without claiming to be an application framework until it
defines a coherent lifecycle. ADR-0001, ADR-0004, and ADR-0005 remain in effect.

## Consequences

### Positive Consequences

- The project can grow without competing with the platform.
- Package boundaries stay justified by responsibility, not ambition.
- Users can adopt one module without adopting the whole ecosystem.
- Generated code remains ordinary, editable C#.
- The CLI can offer strong conventions without forcing an architecture.

### Negative Consequences

- Laranet will not provide a single "batteries-included" experience in the short term.
- Users must compose some pieces themselves.
- The identity requires clear documentation to avoid being mistaken for a full
  framework.
- The "framework" positioning question will need to be revisited at `1.0.0`.

## Confirmation

This decision is validated by:

- Packages that can be installed independently.
- Templates and generators that build on native .NET projects.
- No mandatory runtime metadata or base classes in generated code.
- Documentation that distinguishes Laranet from ASP.NET Core and EF Core.
- An explicit review before any `1.0.0` positioning change.

## Related Decisions

- [ADR-0001: Keep the Core Independent](0001-independent-core.md)
- [ADR-0002: Use Optional Framework Integrations](0002-optional-integrations.md)
- [ADR-0004: Generate Ordinary, Editable C#](0004-editable-generated-code.md)
- [ADR-0005: Keep the Core Package Intentionally Empty](0005-intentionally-empty-core.md)
