# Design Principles

## 1. Introduction

Laranet is designed around a set of principles that guide its architecture, API design, implementation, and long-term evolution.

These principles exist to ensure that Laranet remains useful, maintainable, extensible, and consistent with the .NET ecosystem.

They are not intended to impose unnecessary restrictions. Instead, they provide a common foundation for making technical decisions.

## 2. Developer Experience First

Laranet exists to improve the developer experience.

Its APIs should reduce unnecessary boilerplate, simplify common operations, and make application code easier to read.

A good API should:

- Be intuitive to discover.
- Communicate its purpose through naming.
- Require minimal configuration when reasonable.
- Provide predictable behavior.
- Avoid unnecessary complexity.

Inspired by Laravel, Laranet values expressive APIs and developer productivity.

However, expressiveness should never compromise clarity or type safety.

## 3. Native .NET Integration

Laranet should feel like a natural extension of the .NET ecosystem.

It should embrace established .NET conventions instead of introducing alternative mechanisms without a strong reason.

This includes:

- Dependency Injection through established .NET abstractions.
- Asynchronous programming with Task and ValueTask where appropriate.
- LINQ and expression trees when applicable.
- Nullable reference types.
- Standard .NET exception handling.
- Compatibility with existing libraries and tooling.

Laranet should complement the ecosystem rather than compete with it.

## 4. Simplicity and Pragmatism

Prefer the simplest solution that correctly solves the problem.

Avoid introducing abstractions, dependencies, configuration options, or extension points without a concrete use case.

The following principles guide implementation:

- KISS: Keep implementations straightforward.
- YAGNI: Do not implement speculative functionality.
- DRY: Avoid unnecessary duplication without creating premature abstractions.

Simplicity does not mean avoiding abstraction. It means ensuring that every abstraction has a clear purpose.

## 5. Composition Over Coupling

Modules should interact through well-defined contracts whenever abstraction provides meaningful value.

Implementations should not depend unnecessarily on concrete details of unrelated modules.

Prefer composition and dependency injection over rigid inheritance hierarchies.

However, interfaces should not be introduced merely to wrap every class. Abstraction should solve a real design problem.

## 6. Modularity and Optional Dependencies

Laranet should allow developers to adopt only the functionality they need.

The Core module must remain independent of optional integrations.

Framework-specific and provider-specific dependencies should be isolated within their respective modules.

Applications should not be forced to install unnecessary packages to access unrelated functionality.

## 7. Expressive but Predictable APIs

Laranet APIs should favor readability and consistency.

For example, operations such as searching, sorting, and pagination should communicate their intent clearly.

API design should consider:

- Naming consistency.
- Method chaining where it improves readability.
- Explicit behavior.
- Strong typing.
- Meaningful return types.
- Clear asynchronous semantics.

Convenience methods must not hide unexpected side effects or expensive operations.

## 8. Performance Awareness

Developer experience must not come at the expense of unnecessary runtime overhead.

Implementations should avoid:

- Unnecessary allocations.
- Repeated expression compilation.
- Redundant query execution.
- Unnecessary materialization of collections.
- Reflection when a simpler alternative is available.

Performance decisions should be supported by measurement when optimization becomes relevant.

Correctness and maintainability remain priorities over speculative micro-optimizations.

## 9. Compatibility and Stability

Public APIs are long-term contracts.

Changes to public behavior should be evaluated carefully.

Laranet follows Semantic Versioning and maintains its own compatibility policy independently of .NET release numbering.

Breaking changes should be documented and accompanied by migration guidance when necessary.

## 10. Testability

Features should be designed with testing in mind.

Tests should validate observable behavior rather than implementation details.

The project should prioritize:

- Unit tests for isolated behavior.
- Integration tests for interactions between components.
- Compatibility tests for supported integrations.
- Regression tests for previously identified defects.

Testing should provide confidence without making the architecture unnecessarily complicated.

## 11. Minimal Dependencies

External dependencies should be introduced only when they provide meaningful value.

Before adding a dependency, evaluate:

- Whether the functionality can reasonably be implemented using existing .NET capabilities.
- Maintenance and release activity.
- Compatibility with supported frameworks.
- Licensing.
- Security implications.
- Runtime and distribution impact.

Laranet should avoid unnecessary dependency chains.

## 12. Evolution Through Real Requirements

Laranet is expected to evolve through practical usage, feedback, and implementation experience.

New abstractions and modules should emerge from concrete requirements rather than hypothetical future scenarios.

Significant design decisions should be documented and reviewed as the project grows.

## 13. Final Principle

Laranet should make common .NET development tasks more enjoyable without hiding the strengths of the platform.

**Make .NET feel better to work with, not less like .NET.**
