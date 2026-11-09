# Modularity

## 1. Introduction

Laranet follows a modular architecture designed to separate responsibilities, isolate optional integrations, and minimize unnecessary dependencies.

The objective is to allow developers to adopt individual capabilities without requiring the entire toolkit.

Modules should have clearly defined responsibilities and explicit dependency boundaries.

## 2. Modular Architecture

Laranet is organized around a small foundational Core and additional modules that provide specialized functionality.

The initial logical organization is:

```text
Laranet
│
├── Core
│
├── Data
│
├── EntityFrameworkCore
│
├── AspNetCore
│
├── Cli
│
└── Application
```

This is an architectural direction, not a requirement to implement every module immediately.

Each module must justify its existence through a clear responsibility or dependency boundary.

## 3. Core Module

Core provides the fundamental abstractions and shared functionality required by other Laranet modules.

### Responsibilities

- Fundamental abstractions.
- Shared contracts.
- Common functionality.
- Technology-independent extension points.

### Restrictions

Core must not reference:

- Entity Framework Core.
- ASP.NET Core.
- Specific database providers.
- Infrastructure implementations from optional modules.

Core should remain small and stable.

## 4. Data Module

Data provides reusable functionality for working with data and composing queries.

Its initial scope includes:

- Searching and filtering.
- Sorting.
- Pagination.
- Query composition utilities.

Data should not require a particular database provider.

When an operation needs provider-specific behavior, that responsibility should be delegated to an appropriate integration.

The implementation must preserve the capabilities of the underlying query provider whenever possible.

## 5. Optional Integrations

Integrations connect Laranet with external frameworks and technologies.

They should provide framework-specific functionality without introducing mandatory dependencies into unrelated modules.

### EntityFrameworkCore

Provides Entity Framework Core compatibility.

Responsibilities include:

- Query provider integration.
- Expression compatibility.
- Asynchronous query execution.
- EF Core-specific extensions.

### AspNetCore

Provides ASP.NET Core compatibility.

Potential responsibilities include:

- Dependency injection registration helpers.
- Framework-specific configuration.
- HTTP-related integrations.
- ASP.NET Core application extensions.

Additional integrations may be introduced when practical requirements justify them.

## 6. Application Module

Application may contain higher-level application development utilities.

Potential responsibilities include:

- Validation abstractions.
- Application-level patterns.
- Reusable application services.
- Error handling utilities.

This module is not mandatory for the initial release.

Its implementation will depend on actual use cases and the need for a separate responsibility boundary.

## 7. CLI Module

The CLI provides command-line tooling for creating and extending Laranet applications.

Its initial scope includes:

- Project creation from built-in templates.
- Optional package references during project creation.
- Code generation for models, services, controllers, middleware, resources, and optional repository scaffolds.
- Conventional output folders.
- Namespace inference from the nearest project.
- User-defined templates discovered in a `.laranet/templates` directory.

The CLI generates normal, editable C# and must not introduce runtime dependencies or mandatory metadata into generated applications.

New commands should build on existing .NET tooling whenever it provides the required behavior.

## 8. Dependency Boundaries

The following rules define the intended module boundaries:

1. Core must not depend on optional modules.
2. Data must not require Entity Framework Core.
3. EntityFrameworkCore may depend on Data and Core when necessary.
4. AspNetCore may depend on Core and other modules when integration requires it.
5. Application must not introduce framework dependencies into Core.
6. Optional integrations must remain independently installable whenever technically feasible.
7. Runtime modules must not depend on the CLI.

Dependencies should be explicit and justified.

### Conceptual Dependency Diagram

```text
                Application
                     |
                     v
                 Data Module
                     |
                     v
                 Core Module
                     ^
                     |
          +----------+----------+
          |                     |
          |                     |
    EntityFrameworkCore     AspNetCore
```

This diagram represents conceptual relationships. The final project references will be determined by the actual public contracts and implementation requirements.

## 9. Package Distribution

Modules intended for independent consumption should be distributed as separate NuGet packages.

The initial package names are expected to follow this convention:

| Module                | Expected Package            |
| --------------------- | --------------------------- |
| Core                  | Laranet                     |
| Data                  | Laranet.Data                |
| Entity Framework Core | Laranet.EntityFrameworkCore |
| ASP.NET Core          | Laranet.AspNetCore          |
| Command line          | Laranet.Cli                 |
| Application           | Laranet.Application         |

Package names may change before the first stable release.

Not every logical module necessarily needs its own package or project.

Package boundaries should reflect actual dependency and distribution requirements.

## 10. Adding New Modules

A new module should only be introduced when at least one of the following conditions is met:

- It represents a distinct responsibility.
- It introduces optional external dependencies.
- It needs independent distribution.
- It has a meaningful independent lifecycle.
- Keeping it inside an existing module would create unnecessary coupling.

Before creating a module, evaluate whether the functionality belongs in an existing module.

Avoid splitting functionality into projects solely for organizational purposes.

## 11. Integration Guidelines

Integrations should follow these guidelines:

- Keep external dependencies isolated.
- Reuse existing .NET abstractions whenever possible.
- Avoid duplicating functionality already provided by the ecosystem.
- Preserve native framework behavior.
- Document supported versions.
- Test compatibility against supported dependency versions.

Integrations should extend Laranet rather than redefine the behavior of the frameworks they connect to.

## 12. Architectural Evolution

The modular structure is expected to evolve as Laranet gains functionality.

New modules, integrations, or abstractions must be evaluated against the project's design principles.

Significant changes to module responsibilities or dependency boundaries should be documented through Architecture Decision Records.

## 13. Final Principle

Modularity exists to reduce coupling and improve flexibility, not to increase project complexity.

**Every module must have a clear reason to exist.**
