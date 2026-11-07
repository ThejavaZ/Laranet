# AGENTS.md

## Laranet — Agent Instructions

## Project Overview

Laranet is a Laravel-inspired toolkit for .NET, bringing expressive APIs, developer-friendly abstractions, and elegant patterns to the C# ecosystem.

**Guiding philosophy:**

> Don't make .NET look like Laravel. Make .NET feel better to work with.

Laranet is a toolkit, not a framework. It must improve developer experience without replacing or fighting the native .NET ecosystem.

The project prioritizes:

- Developer experience (DX).
- Expressive and discoverable APIs.
- Simplicity and maintainability.
- Extensibility and modularity.
- Native .NET conventions and compatibility.
- Minimal dependencies.

## Core Principles

1. **Stay idiomatic to .NET.** Use C#, LINQ, dependency injection, and existing .NET conventions naturally.
2. **Do not reinvent existing functionality without a clear reason.**
3. **Avoid unnecessary abstractions.** Every abstraction must solve a demonstrated problem.
4. **Keep APIs expressive and predictable.**
5. **Prefer composition over unnecessary inheritance.**
6. **Preserve provider compatibility whenever working with IQueryable.**
7. **Do not introduce breaking changes casually.**
8. **Keep integrations optional and decoupled.**

Do not introduce patterns such as Repository, Unit of Work, CQRS, Result, or custom exception hierarchies simply because they are common architectural practices. Introduce them only when justified by a real use case.

## Architecture

Laranet is organized into independent modules.

Current projects:

- `Laranet`: Core toolkit.
- `Laranet.Data`: Provider-neutral data and IQueryable utilities.

Planned integrations may include:

- `Laranet.EntityFrameworkCore`
- `Laranet.AspNetCore`
- `Laranet.Cli`

These modules must remain optional and independently maintainable.

### Dependency Rules

- Core must not depend on ASP.NET Core or Entity Framework Core.
- `Laranet.Data` must not depend on Entity Framework Core.
- Integrations may depend on Core and the libraries they explicitly integrate with.
- Avoid circular dependencies.
- Do not add projects or modules without a concrete responsibility.

Do not implement planned modules before their requirements and responsibilities have been established.

## Technology

- Target framework: .NET 10 (`net10.0`).
- Language: C#.
- Nullable reference types enabled.
- Implicit usings enabled.
- Central Package Management is used.
- XML documentation is enabled.
- Warnings are treated as errors.

Follow the existing project configuration instead of introducing conflicting settings.

## Coding Conventions

- Use clear, descriptive names.
- Follow standard C# naming conventions.
- Prefer explicit and readable implementations over clever code.
- Use nullable reference types correctly.
- Validate public API arguments where appropriate.
- Document public APIs using XML documentation.
- Keep methods focused on a single responsibility.
- Avoid unnecessary comments that simply repeat the code.
- Avoid unnecessary dependencies.

Do not suppress compiler warnings without understanding and documenting why the suppression is necessary.

## API Design

Public APIs are part of the product.

Before introducing a public API, consider:

- Is the API intuitive?
- Is its behavior predictable?
- Are its defaults sensible?
- Does it follow existing .NET conventions?
- Does it introduce unnecessary complexity?
- Can it be composed with existing .NET APIs?
- Does it create long-term compatibility obligations?

Prefer stable and simple contracts over excessive configurability.

Do not expose implementation details unnecessarily.

## IQueryable and Data Operations

When working with `IQueryable<T>`:

- Preserve query composition.
- Use expression trees when provider translation is required.
- Do not materialize queries prematurely.
- Do not switch to in-memory execution without an explicit reason.
- Avoid provider-specific APIs in provider-neutral modules.
- Do not assume every IQueryable provider behaves like Entity Framework Core.

Provider-specific asynchronous execution belongs in the appropriate integration module.

## Dependencies

Before adding a package:

1. Check whether the functionality already exists in .NET.
2. Evaluate whether the dependency is necessary.
3. Consider its maintenance, compatibility, and transitive dependencies.
4. Keep dependencies scoped to the module that needs them.

Do not add dependencies merely for convenience when a small, maintainable implementation is sufficient.

## Testing

Every new behavior should have appropriate tests.

Use:

- Unit tests for contracts, validation, and isolated behavior.
- Integration tests when provider translation or external infrastructure behavior must be verified.

The test project uses xUnit and SQLite for database integration tests.

Tests should:

- Be deterministic.
- Be independent.
- Use descriptive names.
- Verify observable behavior.
- Cover relevant edge cases.
- Avoid testing private implementation details.

Do not modify tests simply to make an incorrect implementation pass.

## Documentation

Keep documentation aligned with the actual implementation.

Do not document planned functionality as if it already exists.

Prefer documenting established decisions over speculative future designs.

## Contribution Workflow

Before modifying code:

1. Inspect the relevant project and existing implementation.
2. Understand the current public API and architectural constraints.
3. Identify the actual problem or requirement.
4. Propose a minimal solution.

After modifying code:

1. Review the complete changes.
2. Build the solution.
3. Run relevant tests.
4. Run the full test suite when appropriate.
5. Review documentation and public API impact.
6. Summarize changes, decisions, and remaining concerns.

Do not claim that tests or builds passed unless they were actually executed.

Do not make unrelated changes while implementing a feature.

Do not commit or push changes unless explicitly requested.

## Working With AI

AI agents must:

- Read the applicable repository instructions before making changes.
- Inspect existing implementations instead of assuming their structure.
- Ask for clarification when requirements are ambiguous and materially affect the design.
- Explain significant architectural decisions.
- Distinguish actual defects from optional improvements.
- Avoid inventing requirements.
- Avoid unnecessary refactoring.
- Preserve the project's established philosophy.

The objective is not to maximize the amount of generated code. It is to improve Laranet with well-designed, maintainable functionality.
