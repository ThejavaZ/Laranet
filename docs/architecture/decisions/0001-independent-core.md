# ADR-0001: Keep the Core Independent

- **Status:** Accepted
- **Date:** 2026-11-05

## Context and Problem Statement

Laranet aims to provide reusable abstractions and developer-friendly functionality across the .NET ecosystem.

A central architectural concern is how to prevent the foundational components from becoming coupled to specific application frameworks, database technologies, or infrastructure implementations.

If the Core depends on technologies such as ASP.NET Core or Entity Framework Core, applications that do not use those technologies may inherit unnecessary dependencies.

How can Laranet provide a reusable foundation without forcing applications to adopt specific frameworks or infrastructure?

## Decision Drivers

- Technology independence.
- Minimal dependencies.
- Reusability across different application types.
- Long-term maintainability.
- Compatibility with the existing .NET ecosystem.
- Independent evolution of optional integrations.

## Considered Options

- Build a Core dependent on ASP.NET Core and Entity Framework Core.
- Build a Core independent of external framework integrations.
- Avoid a shared Core and duplicate abstractions across modules.

## Decision Outcome

Chosen option: **Build a Core independent of external framework integrations.**

Laranet Core must contain only fundamental abstractions and shared functionality that do not require specific application frameworks or infrastructure technologies.

It must not directly depend on:

- ASP.NET Core.
- Entity Framework Core.
- Specific database providers.
- Infrastructure implementations from optional integrations.

Integrations may depend on Core when necessary, but Core must never depend on those integrations.

## Consequences

### Positive Consequences

- Applications can use Core without adopting ASP.NET Core or EF Core.
- Framework integrations can evolve independently.
- Dependency chains remain smaller and easier to understand.
- Core becomes easier to test and maintain.
- Laranet can support different application architectures.

### Negative Consequences

- Some functionality may require additional integration packages.
- Shared abstractions must be designed carefully to avoid unnecessary coupling.
- Integration projects may require additional implementation and testing effort.

## Confirmation

This decision will be validated through:

- Project dependency references.
- Automated architecture tests where appropriate.
- Dependency analysis during code reviews.
- Ensuring Core does not reference optional framework packages.

## Related Decisions

- [ADR-0002: Use Optional Framework Integrations](0002-optional-integrations.md)
- [Architecture Overview](../overview.md)
- [Modularity](../modularity.md)
