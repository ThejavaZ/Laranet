# ADR-0007: Default to the Simplest Generation Profile

- **Status:** Accepted
- **Date:** 2026-11-08

## Context and Problem Statement

Laranet's CLI can generate projects and code for different architectural styles. A
generator that produces repositories, service interfaces, DTOs, validators, and
mappers for every CRUD operation may look "professional" while adding maintenance
cost that a small application does not need.

The default profile determines what most users receive. It therefore shapes how
Laranet is perceived and how much unnecessary code enters their projects.

## Decision Drivers

- Avoid over-engineering generated code.
- Keep the generated result easy to understand, edit, and delete.
- Respect the project's rule against unnecessary abstractions.
- Support layered and clean architectures when the user chooses them.
- Keep every profile testable and documented.
- Preserve native EF Core and ASP.NET Core usage.

## Considered Options

- Default to a layered or clean architecture with repositories and interfaces.
- Default to a minimal implementation with direct framework usage.
- Require the user to choose a profile every time.

## Decision Outcome

Chosen option: **Default to the `simple` profile and make richer profiles opt-in.**

Profiles:

| Profile | Default | Characteristics |
| --- | --- | --- |
| `simple` | Yes | Endpoint or controller, request/response DTOs, direct `DbContext` usage, validation, and tests. No repository, service interface, or unit of work. |
| `standard` | No | Adds a service class and mapping when the operation is more than a thin CRUD. Interfaces only at real boundaries. |
| `clean` | No | Explicit layers, interfaces, and repository or use-case boundaries. Chosen deliberately. |
| `modular` | No | Feature folders with the same level of abstraction as `standard`. |

Rules:

- No profile generates `Result<T>`, `Error`, Repository, Unit of Work, CQRS, or a
  mediator unless the user explicitly selects a profile that requires them.
- Interfaces are generated only when they represent a real boundary, substitution
  point, or test seam.
- Every generated file must be ordinary, editable C#.
- Every profile must build, run, and be covered by tests before it ships.
- The CLI reports which profile was used and what was generated.

## Consequences

### Positive Consequences

- New users receive the smallest useful implementation.
- Generated projects do not start with speculative layers.
- Richer architectures remain available for users who need them.
- The default reinforces the project's "no unnecessary abstractions" principle.

### Negative Consequences

- Users who expect a full Clean Architecture scaffold must pass an explicit option.
- Maintaining several profiles increases the generator test matrix.
- Documentation must explain the trade-offs of each profile.

## Confirmation

This decision is validated by:

- `make:crud` output tests for every profile.
- Generated projects that build without warnings.
- Documentation that shows the files produced by each profile.
- No default generation of repositories, unit-of-work, `Result`, or mediator types.

## Related Decisions

- [ADR-0004: Generate Ordinary, Editable C#](0004-editable-generated-code.md)
- [ADR-0006: Position Laranet as a Modular Toolkit with Framework-like Tooling](0006-toolkit-with-framework-tooling.md)
