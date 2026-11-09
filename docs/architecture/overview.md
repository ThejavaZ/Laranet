# Architecture Overview

## 1. Introduction

Laranet is a Laravel-inspired toolkit for .NET focused on improving developer experience through expressive APIs, reusable abstractions, and developer-friendly patterns.

Its architecture is designed to provide a balance between:

- Developer productivity.
- Maintainability.
- Extensibility.
- Performance.
- Native .NET integration.

Laranet does not aim to replace the .NET ecosystem or reproduce Laravel internally. Instead, it builds upon existing .NET capabilities to provide a more expressive and productive development experience.

## 2. Architectural Vision

The central architectural principle of Laranet is **independence through modularity**.

The core of Laranet must remain independent of specific application frameworks, database providers, and infrastructure technologies.

Integrations should extend the toolkit without forcing unnecessary dependencies on applications that do not need them.

For example:

- Applications should be able to use Laranet without ASP.NET Core.
- Applications should be able to use Laranet without Entity Framework Core.
- Database integrations should be optional.
- Framework-specific functionality should remain isolated from the core.

This approach allows Laranet to evolve without becoming tightly coupled to a particular technology.

## 3. Architectural Structure

Laranet follows a modular architecture organized around a small core and optional extensions.

The initial architectural structure is expected to evolve into the following organization:

```text
Laranet
│
├── Core
│   └── Fundamental abstractions and shared functionality
│
├── Data
│   └── Data access abstractions and expressive query utilities
│
├── EntityFrameworkCore
│   └── Entity Framework Core integration
│
├── AspNetCore
│   └── ASP.NET Core integration
│
├── Cli
│   └── Command-line code generation
│
└── Application
    └── Application-level utilities and patterns
```

This represents the intended logical architecture, not a requirement to create every package immediately.

Modules will only become separate projects when there is a clear architectural or distribution reason.

### 3.1 Core

The Core module contains the fundamental abstractions and functionality shared across Laranet.

Its responsibilities include:

- Common abstractions.
- Shared utilities.
- Fundamental extension points.
- Technology-independent functionality.

Core must not depend on:

- ASP.NET Core.
- Entity Framework Core.
- Specific database providers.
- External infrastructure implementations.

The Core module should remain small, stable, and focused.

### 3.2 Data

The Data module provides expressive APIs and reusable utilities for working with data.

Its initial scope includes:

- Query searching and filtering.
- Sorting.
- Pagination.
- Reusable query extensions.

The Data module should avoid unnecessary dependencies on specific database implementations.

Database-specific behavior should be provided through dedicated integrations when required.

### 3.3 Entity Framework Core Integration

The EntityFrameworkCore module provides integration with Entity Framework Core.

Its responsibilities include:

- Translating Laranet data operations into EF Core-compatible expressions.
- Supporting asynchronous query execution.
- Preserving database-side query execution.
- Providing EF Core-specific extensions.

The integration must not become a mandatory dependency of the Data module.

### 3.4 ASP.NET Core Integration

The AspNetCore module provides functionality specifically designed for ASP.NET Core applications.

Potential responsibilities include:

- Dependency injection registration helpers.
- Application configuration extensions.
- HTTP-related integrations.
- Framework-specific utilities.

ASP.NET Core functionality must remain isolated from the Core module.

### 3.5 Application

The Application module may provide higher-level application development utilities.

Potential responsibilities include:

- Validation abstractions.
- Application-level patterns.
- Error handling utilities.
- Reusable application services.

Its scope will be determined by actual use cases rather than speculative abstractions.

### 3.6 Command-Line Tooling

The CLI module provides command-line code generation for Laranet applications.

Its initial scope includes:

- Creating Web API and console projects.
- Choosing between project and repository layouts, with optional solutions.
- Generating models, services, controllers, middleware, resources, and optional repository scaffolds.
- Inferring namespaces and using conventional folders.
- Overriding built-in templates from a `.laranet/templates` directory.
- Optional interactive prompts for missing values and overwrite confirmation.

The CLI must generate normal, editable C# and must not introduce mandatory runtime metadata.

## 4. Dependency Rules

Laranet follows a dependency direction that prioritizes independence and minimizes coupling.

The fundamental rules are:

1. Core must not depend on optional modules.
2. Integrations may depend on Core when necessary.
3. Framework-specific dependencies must remain within their corresponding integrations.
4. Optional integrations must not become mandatory dependencies of unrelated modules.
5. Dependencies should point toward stable abstractions whenever practical.

Conceptually:

```text
             Application
                  |
                  v
             Laranet Core
                  ^
                  |
       +----------+----------+
       |                     |
       v                     v
   Laranet Data       Framework Integrations
       |                     |
       v                     +---- ASP.NET Core
   EF Core Integration      |
                             +---- Other Integrations
```

The diagram represents logical relationships, not a finalized project dependency graph. Exact references will be established when the first projects are implemented.

## 5. Design Principles

Laranet's architecture follows these principles:

### 5.1 Simplicity First

Prefer straightforward solutions over unnecessary abstractions.

A feature should justify its complexity through practical value.

### 5.2 Developer Experience

APIs should be expressive, predictable, and easy to understand.

Common operations should not require excessive boilerplate.

### 5.3 Native .NET Integration

Laranet should feel natural within the .NET ecosystem.

It should work with established .NET conventions, tooling, dependency injection, asynchronous programming, and language features.

### 5.4 Modularity

Applications should only need the functionality they actually use.

Optional integrations must remain optional.

### 5.5 Extensibility

Developers should be able to extend Laranet without modifying its internal implementation.

Public abstractions should provide clear extension points.

### 5.6 Performance

Expressive APIs must not introduce unnecessary runtime overhead.

When working with data, query composition and execution should preserve provider capabilities whenever possible.

### 5.7 Maintainability

Public APIs should remain consistent, documented, and carefully designed.

Breaking changes must be considered deliberately and communicated through versioning and migration documentation.

## 6. Technology Strategy

Laranet is built for the .NET ecosystem.

The initial development target is:

- .NET 10.
- C#.
- NuGet package distribution.

Additional integrations and supported target frameworks may be introduced as the project evolves.

Laranet will follow its own independent versioning policy rather than matching its release numbers to .NET versions.

See the [Compatibility and Versioning](../compatibility/versioning.md) documentation for more information.

## 7. Public API Philosophy

Public APIs are one of the most important architectural boundaries in Laranet.

The project aims to provide APIs that are:

- Expressive without being ambiguous.
- Consistent across modules.
- Easy to discover.
- Predictable in behavior.
- Extensible without exposing unnecessary implementation details.

Before introducing a public abstraction, its purpose, ownership, and long-term maintenance cost should be evaluated.

Internal implementation details should remain internal unless exposing them provides a meaningful extension or integration benefit.

## 8. Evolution of the Architecture

Laranet is currently in early development.

Its architecture will evolve through implementation, testing, and feedback.

Architectural changes should preserve the project's fundamental goals:

- Core independence.
- Optional integrations.
- Minimal coupling.
- Developer-friendly APIs.
- Native .NET conventions.

Significant architectural decisions should be documented through Architecture Decision Records (ADRs).

The architecture should evolve in response to real problems rather than speculative future requirements.

## 9. Summary

Laranet is built around a simple idea:

**Make .NET feel better to work with without compromising what makes .NET powerful.**

Its architecture prioritizes a small independent core, optional integrations, expressive APIs, and deliberate design decisions.

The objective is not to build another application framework that dictates how developers must work, but to provide tools that make their existing .NET development experience better.
