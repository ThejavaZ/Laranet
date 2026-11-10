# Laranet — Roadmap

> A Laravel-inspired toolkit for .NET, bringing expressive APIs, developer-friendly abstractions, and elegant patterns to the C# ecosystem.

**Guiding principle:** Don't make .NET look like Laravel. Make .NET feel better to work with.

## Version roadmaps

The roadmap is organized by release so completed and future work stay separated.

| Version | Scope | Status |
| --- | --- | --- |
| [Version 1](v1.md) | Core, Data, EntityFrameworkCore, AspNetCore, and CLI; first public release | Released `0.1.0`; `0.1.1` prepared |
| [Version 2](v2.md) | Adoption, provider verification, feedback-gated depth, and 1.0 readiness | Planning |
| [Version 3](v3.md) | Application experience: standards, templates, CRUD generation, EF conventions, validation, and doctor | Planning |
| [Version 4](v4.md) | Application surface: optional admin panel, web/Blazor templates, and optional adapters | Candidate |
| [Version 5](v5.md) | First stable release: `1.0.0`, release integrity, data experience, diagnostics, and enterprise template | Planned (gated by Version 4) |
| [Version 6](v6.md) | Post-`1.0` application capabilities in `1.x`: modules, resources, events, jobs, authentication, and permissions | Direction (not scheduled) |

## Project vision

Laranet aims to improve the developer experience in the .NET ecosystem through expressive APIs, reusable abstractions, productivity tools, and optional integrations.

It should:

- Reduce repetitive and unnecessarily verbose code.
- Preserve C#'s strong typing and .NET conventions.
- Work naturally with LINQ, Dependency Injection, EF Core, and ASP.NET Core.
- Provide useful conventions without imposing mandatory architecture.
- Generate ordinary, editable C# code.
- Keep integrations modular and independently consumable.

Laranet is a **toolkit, not an opinionated application framework**.

## Non-negotiable principles

1. **Native .NET first:** use existing platform capabilities instead of reinventing them.
2. **Developer experience:** APIs should be intuitive, consistent, and easy to discover.
3. **Modularity:** optional integrations must not become mandatory dependencies.
4. **Provider neutrality:** generic data abstractions must not depend on a specific database provider.
5. **No unnecessary abstractions:** don't introduce repositories, units of work, result wrappers, or base classes without a concrete justification.
6. **Editable generated code:** CLI output must be normal C#.
7. **Testability:** public functionality needs unit tests and integration tests where applicable.
8. **Documentation:** public APIs need examples and clear behavior contracts.
9. **Compatibility:** package dependencies and supported .NET versions must be explicit.
10. **Incremental development:** every feature must justify its place in the toolkit.

## Planned package ecosystem

| Package                       | Responsibility                                   | Status                     |
| ----------------------------- | ------------------------------------------------ | -------------------------- |
| `Laranet`                     | Core abstractions and shared functionality       | Published `0.1.0`; `0.1.1` prepared; intentionally empty |
| `Laranet.Data`                | Query extensions, pagination, searching, sorting | Published `0.1.0`; `0.1.1` prepared |
| `Laranet.EntityFrameworkCore` | EF Core integration and async query execution    | Published `0.1.0`; `0.1.1` prepared |
| `Laranet.AspNetCore`          | ASP.NET Core integration                         | Published `0.1.0`; `0.1.1` prepared |
| `Laranet.Cli`                 | Project and code generation commands             | Published `0.1.0`; `0.1.1` prepared |
| `Laranet.AspNetCore.Validation` | Optional request validation integration        | Planned for Version 3     |
| `Laranet.Analyzers`           | Optional Roslyn diagnostics                      | Planned for Version 5     |
| `Laranet.Admin`               | Admin resource abstractions                      | Candidate for Version 4   |
| `Laranet.Admin.AspNetCore`    | Razor-based admin UI                             | Candidate for Version 4   |
| `Laranet.Modules`             | Modular monolith composition                     | Version 6 candidate; not scheduled |
| `Laranet.Application`         | Application-level utilities, only if justified   | Not scheduled             |

Packages should remain independently consumable. Shared package versions follow
[ADR-0008](../architecture/decisions/0008-runtime-and-tooling-versioning.md): one
release train by default, with a documented exception process.

## Branding and identity

- [x] Incorporate the Laranet logo into the README.
- [x] Establish a consistent visual identity.
- [x] Add package and documentation branding.
- [x] Prepare logo assets in appropriate formats.
- [x] Document logo usage and asset locations.

See [Branding](../branding.md) for the asset inventory and usage guidelines.

## AI-assisted development workflow

AI tools such as DeepSeek, Claude, and OpenCode can assist with research, implementation, code review, and testing.

They must follow this workflow:

1. **Understand:** inspect the relevant source code, project instructions, and architecture.
2. **Analyze:** identify the problem and its actual causes.
3. **Propose:** present the intended changes before implementing them.
4. **Review:** explain affected files, design decisions, dependencies, risks, and tests.
5. **Authorize:** wait for explicit approval from the project maintainer.
6. **Implement:** modify only the approved scope.
7. **Verify:** execute relevant builds and tests.
8. **Report:** summarize modifications, test results, and remaining concerns.

AI tools must not:

- Change architectural decisions without approval.
- Add dependencies without justification.
- Introduce unnecessary abstractions.
- Modify unrelated files.
- Remove tests to make a build pass.
- Claim tests passed without executing them.
- Treat their own recommendations as project requirements.

## Feature completion criteria

A feature is not complete merely because it compiles.

Before marking it complete, verify:

- [ ] Public API and behavior are defined.
- [ ] Implementation follows project principles.
- [ ] Unit tests cover expected behavior and edge cases.
- [ ] Integration tests exist where provider behavior matters.
- [ ] XML documentation is complete.
- [ ] Dependencies are justified.
- [ ] No unintended architectural coupling exists.
- [ ] Build and tests pass.
- [ ] Documentation and changelog are updated when appropriate.

## Current project state

**Current milestone:** [Version 2, Sprint 9 — Post-release adoption](v2.md).
Sprint 10 — Provider confidence is completed and verified in CI.

All five packages (`Laranet`, `Laranet.Data`, `Laranet.EntityFrameworkCore`,
`Laranet.AspNetCore`, and `Laranet.Cli`) are published on NuGet.org as `0.1.0`. The
repository prepares `0.1.1`, which adds the multi-column `Search` overload, the shared
package icon, and documentation updates. `Laranet.Data` provides provider-neutral
search, sorting, conditional filtering, and pagination, including multi-column search. `Laranet.EntityFrameworkCore` adds
asynchronous pagination, `Laranet.AspNetCore` exposes pagination metadata through
HTTP headers and Minimal API results, and `Laranet.Cli` creates projects and generates
code. Version 2 focuses on adoption and stability: release `0.1.1`, collect real
feedback, and add depth only when a concrete use case justifies it. SQL Server and
PostgreSQL are verified by the provider compatibility suite, which runs in CI with
service containers. The `Laranet` core package remains intentionally empty until a
shared abstraction is required by at least two modules. [Version 3](v3.md) is planned
as the application-experience milestone: standards, project templates, `make:crud`,
EF Core conventions, validation, and `laranet doctor`. Its admin-panel proposal is a
candidate that requires an ADR and a validated use case. [Version 4](v4.md) is a
candidate for the application surface: an optional admin panel, web and Blazor
templates, and optional data adapters, all gated by the Version 3 exit criteria.
[Version 5](v5.md) is the first stable release, planned as `1.0.0`: a small, frozen
runtime surface with release integrity, the Data Experience improvements, a bounded
diagnostics catalog, and an enterprise template. [Version 6](v6.md) is a direction,
not a commitment: modules, API resources, domain events, background jobs, and
permissions, added in `1.x` only when a real application justifies them.

**Long-term objective:** build a modular, extensible, developer-friendly toolkit that makes working with .NET more productive without sacrificing what makes .NET good.
