# Laranet — Project Backlog

> A Laravel-inspired toolkit for .NET, bringing expressive APIs, developer-friendly abstractions, and elegant patterns to the C# ecosystem.

**Guiding principle:** Don't make .NET look like Laravel. Make .NET feel better to work with.

## 1. Project vision

Laranet aims to improve the developer experience in the .NET ecosystem through expressive APIs, reusable abstractions, productivity tools, and optional integrations.

It should:

- Reduce repetitive and unnecessarily verbose code.
- Preserve C#'s strong typing and .NET conventions.
- Work naturally with LINQ, Dependency Injection, EF Core, and ASP.NET Core.
- Provide useful conventions without imposing mandatory architecture.
- Generate ordinary, editable C# code.
- Keep integrations modular and independently consumable.

Laranet is a **toolkit, not an opinionated application framework**.

## 2. Non-negotiable principles

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

## 3. Planned package ecosystem

| Package                       | Responsibility                                   | Status                           |
| ----------------------------- | ------------------------------------------------ | -------------------------------- |
| `Laranet`                     | Core abstractions and shared functionality       | Initial project                  |
| `Laranet.Data`                | Query extensions, pagination, searching, sorting | Implemented, needs stabilization |
| `Laranet.EntityFrameworkCore` | EF Core integration and async query execution    | Planned                          |
| `Laranet.AspNetCore`          | ASP.NET Core integration                         | Planned                          |
| `Laranet.Cli`                 | Project and code generation commands             | Planned                          |
| `Laranet.Application`         | Application-level utilities, only if justified   | Under evaluation                 |

Packages should remain independently consumable. Shared package versions will initially follow the same Laranet release.

## 4. Roadmap

### Sprint 0 — Project foundations

**Status: Completed**

- [x] Repository and solution structure.
- [x] .NET 10 configuration.
- [x] Central Package Management.
- [x] MIT license.
- [x] Architecture documentation.
- [x] Initial architecture decision records.
- [x] CI workflow.
- [x] Contribution guidelines.
- [x] Versioning policy.

### Sprint 1 — Laranet.Data initial implementation

**Status: Implemented; pending stabilization**

- [x] `Search()`
- [x] `SortBy()`
- [x] `Paginate()`
- [x] Dynamic `OrderBy()`
- [x] Dynamic `ThenBy()`
- [x] `WhereIf()`
- [x] `PagedResult<T>`
- [x] Unit tests.
- [x] SQLite integration tests.

### Sprint 2 — Stabilization and package quality

**Priority: High**

- [ ] Remove SQLite package references from production projects.
- [ ] Keep SQLite dependency exclusively in test projects.
- [ ] Validate production package dependency graphs.
- [ ] Complete `Laranet.Data` NuGet metadata.
- [ ] Synchronize package versions to `0.1.0`.
- [ ] Review XML documentation.
- [ ] Document pagination ordering requirements.
- [ ] Document provider-dependent search behavior.
- [ ] Document dynamic sorting and input allowlisting.
- [ ] Document synchronous pagination and its two-query behavior.
- [ ] Correct pagination integration tests to exercise `Paginate()` itself.
- [ ] Review search expression parameterization.
- [ ] Add package generation and dependency checks to CI.
- [ ] Inspect generated `.nupkg` files and their `.nuspec` metadata.
- [ ] Update README and CHANGELOG.

**Definition of done:** production packages contain only their intended dependencies, tests pass, and packages can be built and inspected reliably.

### Sprint 3 — Data API refinement

**Priority: High**

- [ ] Review consistency between expression-based and dynamic sorting APIs.
- [ ] Evaluate expression-based `ThenBy()` overloads.
- [ ] Review pagination behavior for pages beyond the available results.
- [ ] Improve XML documentation and usage examples.
- [ ] Evaluate provider compatibility beyond SQLite.
- [ ] Add reusable integration test support.
- [ ] Review public API naming and consistency before stabilizing the API.

**Important:** don't introduce keyset pagination or additional abstractions without a concrete use case.

### Sprint 4 — Entity Framework Core integration

**Priority: High**

Create `Laranet.EntityFrameworkCore`.

- [ ] Create the independent integration project.
- [ ] Reference `Laranet.Data`.
- [ ] Add EF Core dependencies only to this integration package.
- [ ] Implement `PaginateAsync()`.
- [ ] Preserve provider-side query execution.
- [ ] Support cancellation tokens.
- [ ] Verify generated SQL.
- [ ] Test with SQLite.
- [ ] Evaluate SQL Server and PostgreSQL compatibility.
- [ ] Document supported EF Core versions.

**Architectural constraint:** `Laranet.Data` must remain independent of EF Core.

### Sprint 5 — ASP.NET Core integration

**Priority: Medium**

Create `Laranet.AspNetCore`.

Potential capabilities:

- [ ] Dependency injection extensions where useful.
- [ ] Pagination response integration.
- [ ] HTTP-friendly validation and error handling utilities, if justified.
- [ ] Request/query binding helpers, if they solve a real problem.
- [ ] Integration examples with `Laranet.Data`.
- [ ] Integration tests using ASP.NET Core.

Avoid building a second web framework or replacing native ASP.NET Core features without a strong reason.

### Sprint 6 — Laranet CLI

**Priority: High**

Create `Laranet.Cli`.

The CLI should be inspired by the productivity of Laravel Artisan and NestJS CLI.

**Project creation**

- [ ] `laranet new`
- [ ] Project templates.
- [ ] Configurable project structure.
- [ ] Optional integrations during project creation.

**Code generation**

- [ ] `laranet make:model`
- [ ] `laranet make:service`
- [ ] `laranet make:controller`
- [ ] `laranet make:middleware`
- [ ] `laranet make:resource`
- [ ] `laranet make:repository`, only as an optional template.
- [ ] Additional generators based on actual usage.

**Resource generation**

- [ ] Generate related files together.
- [ ] Allow customization of generated files.
- [ ] Support user-defined templates.
- [ ] Avoid mandatory runtime metadata.

**Developer experience**

- [ ] Clear help and command documentation.
- [ ] Useful error messages.
- [ ] Interactive commands where appropriate.
- [ ] Consistent command naming.
- [ ] Extensible conventions.

### Sprint 7 — Documentation, samples and developer adoption

**Priority: Medium**

- [ ] Create practical sample applications.
- [ ] Document installation and configuration.
- [ ] Add examples for each package.
- [ ] Document architectural decisions.
- [ ] Create migration and upgrade guidance.
- [ ] Prepare NuGet publication.
- [ ] Establish issue and contribution workflows.
- [ ] Gather feedback from .NET developers.

### Sprint 8 — Release preparation

**Priority: Medium**

- [ ] Review all public APIs.
- [ ] Verify package metadata and dependencies.
- [ ] Verify supported .NET versions.
- [ ] Run the full test suite.
- [ ] Build release packages.
- [ ] Review README and documentation.
- [ ] Publish the first public release.
- [ ] Collect feedback and plan the next iteration.

## 5. Branding and identity

- [ ] Incorporate the Laranet logo into the README.
- [ ] Establish a consistent visual identity.
- [ ] Add package and documentation branding.
- [ ] Prepare logo assets in appropriate formats.
- [ ] Document logo usage and asset locations.

## 6. AI-assisted development workflow

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

## 7. Feature completion criteria

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

## 8. Current project state

**Current milestone:** Sprint 2 — Stabilization and package quality.

The initial implementation of `Laranet.Data` is complete. The next objective is to make its public API, dependencies, packaging, and documentation reliable before expanding the toolkit.

**Long-term objective:** build a modular, extensible, developer-friendly toolkit that makes working with .NET more productive without sacrificing what makes .NET good.
