# Contribution Guide

This page explains how the repository is organized and how to prepare a change. The
canonical contribution process — branching, commits, pull requests, and reporting — is
[`CONTRIBUTING.md`](../../CONTRIBUTING.md). Read it first.

## Before you start

Read:

1. [`AGENTS.md`](../../AGENTS.md) for the architectural constraints and project
   philosophy.
2. The [architecture overview](../architecture/overview.md) and the
   [ecosystem architecture](../architecture/ecosystem.md) for package boundaries.
3. The existing tests for the area you want to change.

For a significant change, open an issue first. Small fixes and documentation
corrections can go directly to a pull request.

## Repository map

| Path | Contents |
| --- | --- |
| `src/Laranet` | Core package. Intentionally empty. |
| `src/Laranet.Data` | Provider-neutral query extensions and `PagedResult<T>`. |
| `src/Laranet.EntityFrameworkCore` | Asynchronous EF Core pagination. |
| `src/Laranet.AspNetCore` | Pagination HTTP headers and Minimal API results. |
| `src/Laranet.Cli` | Project creation and code generation tool. |
| `tests/Laranet.Tests` | Unit, integration, and CLI tests. |
| `samples/` | Runnable console and Web API samples. |
| `docs/` | User and contributor documentation. |
| `assets/branding` | Logos and icons. See [Branding](../branding.md). |

## What belongs where

- A provider-neutral query operation belongs in `Laranet.Data`.
- EF Core-specific execution belongs in `Laranet.EntityFrameworkCore`.
- ASP.NET Core-specific behavior belongs in `Laranet.AspNetCore`.
- Project and code generation belongs in `Laranet.Cli`.
- A shared abstraction moves to `Laranet` only when at least two modules need it. See
  [ADR-0005](../architecture/decisions/0005-intentionally-empty-core.md).
- A new module needs an ADR and must satisfy the
  [module admission gates](../architecture/ecosystem.md#9-module-admission-gates).

Do not add a public API only because it is convenient internally. Public APIs are
long-term contracts.

## Proposing a change

Describe:

- The problem, with a concrete use case.
- The chosen solution and the alternatives you rejected.
- The affected packages and whether the dependency graph changes.
- The compatibility impact.
- How the change is tested.
- The documentation that needs to change.

If the change introduces an abstraction, explain what it solves and what it costs. The
project rejects abstractions without a demonstrated problem.

## Implementation checklist

- [ ] The change stays within the approved scope.
- [ ] Public members have XML documentation.
- [ ] Arguments are validated where appropriate.
- [ ] Nullable reference types are respected.
- [ ] Behavior is covered by tests, including edge cases.
- [ ] Provider-dependent behavior is tested against a real provider.
- [ ] No EF Core or ASP.NET Core dependency leaks into `Laranet.Data`.
- [ ] The solution builds without warnings.
- [ ] The full test suite passes.
- [ ] Documentation and `CHANGELOG.md` are updated when behavior changes.

## Documentation contributions

Documentation is part of the product.

- Describe what exists, not what is planned.
- Mark provider-dependent behavior explicitly.
- Keep examples compilable. The samples are built by CI.
- Update the package README when the public API changes.
- Update the guides when behavior or recommendations change.
- Add a changelog entry for user-facing changes.

When adding a page:

1. Add it under the right section (`getting-started`, `concepts`, `guides`, `api`,
   `development`, `compatibility`, or `architecture`).
2. Link it from [`docs/README.md`](../README.md).
3. Verify that every relative link resolves.

## Reviewing a change

A good review verifies:

- The problem is real and the fix addresses it.
- The scope is minimal.
- The public API is justified and documented.
- The tests prove the behavior rather than the implementation.
- No unrelated files or reformatting are included.
- The documentation and changelog match the behavior.

## Related

- [Development workflow](development-workflow.md)
- [Development standards](standards.md)
- [Testing guidelines](testing-guidelines.md)
- [Release process](release.md)
- [`CONTRIBUTING.md`](../../CONTRIBUTING.md)
