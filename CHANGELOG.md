# Changelog

All notable changes to Laranet will be documented in this file.

This project follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

No unreleased changes yet. Planned work is tracked in the
[roadmap](docs/roadmap/README.md).

## [0.1.1] - 2026-11-09

### Added

- Multi-column `Search` overload that matches a term in any of several string properties using a single translated `OR` expression.
- ADR-0005 documenting the decision to keep the `Laranet` core package intentionally empty.
- Ecosystem architecture document defining package boundaries, dependency rules, architectural profiles, and the `0.1.0` → `1.0.0` roadmap.
- ADR-0006 positioning Laranet as a modular toolkit with framework-like tooling.
- ADR-0007 establishing the `simple` generation profile as the default.
- Version 3 roadmap covering standards, project templates, `make:crud`, EF Core conventions, validation, and `laranet doctor`.
- Version 4 roadmap covering the optional admin panel, web and Blazor templates, relationship and adapter research, and ecosystem hardening.
- ADR-0008 defining the runtime and tooling version train, the criteria for `1.0.0`, and the exception process for independent tooling lanes.
- Version 5 roadmap defining `1.0.0` as a stability contract: release integrity, Data Experience improvements, a bounded diagnostics catalog, an enterprise template, and migration documentation.
- Version 6 roadmap documenting post-`1.0` application capabilities as a direction: modules, API resources, events, jobs, permissions, and deferred multi-tenancy.
- Branding assets under `assets/branding` and a shared NuGet package icon for all production packages.
- Documentation for core concepts, dependency injection, extensibility, the public API reference, relationships, validation, error handling, contribution, development workflow, testing guidelines, and branding.
- CI validation that every production package includes the shared icon.

### Changed

- Added `PackageIcon` to the shared NuGet metadata so every production package displays the Laranet icon.
- The README and documentation index now use the Laranet logo and link to the new concepts, API reference, guides, development, and branding sections.

## [0.1.0] - 2026-11-08

### Added

- Initial repository structure and project documentation.
- Repository development conventions and configuration.
- `Laranet.Data` package with `IQueryable` search, sorting, conditional filtering, and pagination utilities.
- Package README for `Laranet.Data`.
- Typed `ThenBy` overload for `IOrderedQueryable<T>` that accepts a `SortDirection`.
- Dynamic `OrderBy` and `ThenBy` overloads that validate property names against an `IReadOnlyCollection<string>` allowlist.
- Provider compatibility documentation for `Laranet.Data`.
- `Laranet.EntityFrameworkCore` package with asynchronous pagination through `PaginateAsync()`.
- Cancellation token support for asynchronous pagination.
- `Laranet.AspNetCore` package with pagination HTTP headers and Minimal API results.
- `Laranet.Cli` .NET tool with `make:model`, `make:service`, `make:controller`, `make:middleware`, `make:resource`, and `make:repository` code generation commands. The repository generator is an optional, intentionally minimal scaffold and is not part of `make:resource`.
- `laranet new` command that creates `webapi` and `console` projects with namespace inference, optional solution-free structure, and optional `Laranet.Data`, `Laranet.EntityFrameworkCore`, and `Laranet.AspNetCore` package references.
- Namespace inference from the nearest project file and conventional output folders for generated code.
- User-defined templates that override the built-in code generators through a `.laranet/templates` directory, using `{{Name}}` and `{{Namespace}}` placeholders.
- Interactive mode (`--interactive`) for project creation and code generators, with prompts for missing names, templates, package references, namespaces, and overwrite confirmation.
- Configurable project layouts (`project` and `repository`) and optional solution generation with `--sln`.
- Runnable console and Web API samples under `samples/`, covering `Laranet.Data`, `Laranet.EntityFrameworkCore`, and `Laranet.AspNetCore`.
- Getting-started guides for installation, quick start, and a complete first application.
- Package overview and practical guides for data access, pagination, and the CLI.
- Migration and upgrade guidance for the initial `0.x` release period.
- ADR-0004 documenting the decision to generate ordinary, editable C#.
- Tag-driven release workflow that validates, packs, and publishes the five production packages to NuGet.org.
- `SECURITY.md` with the private vulnerability reporting process.
- Package README for the `Laranet` core package and CI validation of its NuGet metadata.

### Changed

- Aligned the `Laranet.Data` package version with the core package at `0.1.0`.
- Added NuGet metadata to `Laranet.Data`: description, authors, MIT license, repository, tags, and README.
- Expanded XML documentation for provider-dependent search, sorting, and pagination behavior.
- CI now packs and validates five production packages, including `.nuspec` dependencies, metadata, version consistency, module-specific framework restrictions (EF Core only in `Laranet.EntityFrameworkCore`, ASP.NET Core only in `Laranet.AspNetCore`), and .NET tool packaging for `Laranet.Cli`.
- Expanded the `Laranet.Data` README with sorting, allowlist, conditional filtering, composition, and provider guidance.
- Centralized the SQLite integration test infrastructure in shared test support types.
- Completed the contribution guide with development, testing, commit, pull request, documentation, and security workflows.
- Expanded the documentation index to link only to pages that exist.
- Added `PackageProjectUrl` and `PackageReleaseNotes` to all production packages and CI validation for both.
- Extracted shared NuGet package validation into `.github/scripts/verify-packages.sh`, used by both CI and the release workflow.
- The release workflow now publishes through NuGet Trusted Publishing (`NuGet/login@v1` with GitHub OIDC and a short-lived key) instead of a stored `NUGET_API_KEY` secret.

### Fixed

- Removed Entity Framework Core from the production dependency graph; it is now referenced only by the test project.
- `Search` now sends its term as a provider parameter instead of embedding it as a SQL literal.
- The pagination integration test now exercises `Paginate` and verifies its count and paged queries.
- Corrected the documented meaning of `PagedResult<T>.PageSize` to reflect the effective page size.
- `Paginate` now executes only the count query when the collection is empty or the requested page is out of range, while preserving pagination metadata.

---

## Versioning

Laranet follows Semantic Versioning:

`MAJOR.MINOR.PATCH`

- **MAJOR:** Incompatible public API changes.
- **MINOR:** New functionality introduced in a backward-compatible manner.
- **PATCH:** Backward-compatible bug fixes.

During initial development, the project may use `0.x` versions. Public APIs should be considered subject to change until the first stable release.

## Release History

- `0.1.0` (2026-11-08): first public release of the five production packages.
- `0.1.1` (2026-11-09): multi-column `Search`, shared package icon, and documentation updates.
