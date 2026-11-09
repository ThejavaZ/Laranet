# Laranet — Roadmap

> A Laravel-inspired toolkit for .NET, bringing expressive APIs, developer-friendly abstractions, and elegant patterns to the C# ecosystem.

**Guiding principle:** Don't make .NET look like Laravel. Make .NET feel better to work with.

## Version roadmaps

The roadmap is organized by release so completed and future work stay separated.

| Version | Scope | Status |
| --- | --- | --- |
| [Version 1](v1.md) | Core, Data, EntityFrameworkCore, AspNetCore, and CLI; first public release | In progress |
| [Version 2](v2.md) | Deferred generators, extensible conventions, and feedback-driven work | Planning |

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
| `Laranet`                     | Core abstractions and shared functionality       | Initial project            |
| `Laranet.Data`                | Query extensions, pagination, searching, sorting | Implemented and stabilized |
| `Laranet.EntityFrameworkCore` | EF Core integration and async query execution    | Implemented                |
| `Laranet.AspNetCore`          | ASP.NET Core integration                         | Implemented                |
| `Laranet.Cli`                 | Project and code generation commands             | Implemented                |
| `Laranet.Application`         | Application-level utilities, only if justified   | Under evaluation           |

Packages should remain independently consumable. Shared package versions will initially follow the same Laranet release.

## Branding and identity

- [ ] Incorporate the Laranet logo into the README.
- [ ] Establish a consistent visual identity.
- [ ] Add package and documentation branding.
- [ ] Prepare logo assets in appropriate formats.
- [ ] Document logo usage and asset locations.

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

**Current milestone:** [Version 1, Sprint 7 — Documentation, samples and developer adoption](v1.md).

`Laranet.Data` is stabilized, `Laranet.EntityFrameworkCore` provides optional
asynchronous pagination, `Laranet.AspNetCore` exposes pagination metadata through
HTTP headers and Minimal API results, and `Laranet.Cli` creates projects with
`project` or `repository` layouts and generates models, services, controllers,
middleware, and resources. Sprint 7 adds runnable samples, installation and package
guides, migration guidance, ADR-0004, and a tag-driven NuGet release workflow. The
remaining Sprint 7 item is gathering feedback from .NET developers. Additional
generators and extensible conventions are deferred to [Version 2](v2.md).

**Long-term objective:** build a modular, extensible, developer-friendly toolkit that makes working with .NET more productive without sacrificing what makes .NET good.
