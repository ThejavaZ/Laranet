# ADR-0002: Use Optional Framework Integrations

- **Status:** Accepted
- **Date:** 2026-11-05

## Context and Problem Statement

The .NET ecosystem provides established frameworks and libraries for different application requirements.

ASP.NET Core provides application and HTTP infrastructure, while Entity Framework Core provides object-relational mapping and database access capabilities.

Laranet aims to improve the developer experience when working with these technologies without making them mandatory dependencies.

How should Laranet integrate with external frameworks while preserving modularity and allowing developers to adopt only the functionality they need?

## Decision Drivers

- Optional dependencies.
- Compatibility with existing .NET frameworks.
- Independent package distribution.
- Clear module responsibilities.
- Extensibility.
- Minimal dependency coupling.

## Considered Options

- Include framework integrations directly inside Core.
- Make all framework integrations mandatory dependencies.
- Isolate framework integrations into optional modules.

## Decision Outcome

Chosen option: **Isolate framework integrations into optional modules.**

Laranet will provide dedicated integrations for external frameworks and technologies when their functionality justifies a separate dependency boundary.

The initial planned integrations include:

- **Laranet.EntityFrameworkCore:** Entity Framework Core integration.
- **Laranet.AspNetCore:** ASP.NET Core integration.

These integrations must remain optional and independently installable whenever technically feasible.

### Integration Responsibilities

Entity Framework Core integration is responsible for adapting Laranet data operations to EF Core capabilities, including expression translation and asynchronous query execution.

ASP.NET Core integration is responsible for functionality that requires ASP.NET Core-specific abstractions and infrastructure.

Neither integration should introduce framework dependencies into Core.

## Consequences

### Positive Consequences

- Developers can install only the integrations they need.
- Framework-specific dependencies remain isolated.
- Integrations can evolve independently.
- Laranet can support additional frameworks in the future.
- The Core remains technology-independent.

### Negative Consequences

- More than one NuGet package may be required for certain scenarios.
- Integration compatibility must be maintained and tested.
- Some functionality may need adapters between modules.

## Confirmation

This decision will be validated through:

- Independent package references.
- Dependency graph inspection.
- Integration tests against supported framework versions.
- Verification that applications can use Core without installing optional integrations.

## Related Decisions

- [ADR-0001: Keep the Core Independent](0001-independent-core.md)
- [Architecture Overview](../overview.md)
- [Modularity](../modularity.md)
