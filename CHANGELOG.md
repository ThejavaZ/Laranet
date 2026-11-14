# Changelog

All notable changes to Laranet will be documented in this file.

This project follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `Laranet.Http`, an optional convenience layer over `HttpClient`: typed `GetAsync<T>`, `PostAsync`, `PutAsync`, `PatchAsync`, and `DeleteAsync`; `HttpResult` and `HttpResult<T>` with explicit error kinds; cancellation, per-request timeouts, request and content headers, configurable JSON options, opt-in `ThrowOnError`, and `AddLaranetHttp` registration as a typed client.
- ADR-0010 documenting the HTTP convenience layer, its limits, and its native `HttpClient` escape hatch.
- Opt-in EF Core conventions in `Laranet.EntityFrameworkCore`: `IAuditable` timestamps and `ISoftDeletable` soft delete, registered with `AddLaranetAuditing` and `AddLaranetSoftDelete`, plus `ApplySoftDeleteQueryFilters` for a native global query filter.
- `TimeProvider` support for deterministic auditing and soft-delete timestamps.
- ADR-0009 documenting why Laranet keeps EF Core conventions optional and close to the framework.
- EF Core conventions guide covering auditing, soft delete, filter composition, limitations, and provider differences.
- SQL Server, PostgreSQL, MySQL, and MariaDB provider compatibility tests, selected with `LARANET_TEST_PROVIDER` and backed by unique test databases.
- CI service containers for SQL Server 2022, PostgreSQL 17, MySQL 8.4, and MariaDB 11.4, plus provider-specific test steps.
- Verified provider translation differences for `Search` and verified `DateTimeOffset` storage differences across providers.
- Version 6 candidate for an authentication and security baseline: ASP.NET Core Identity, JWT bearer, and optional BCrypt through `IPasswordHasher<TUser>`.
- Development standards guide covering naming, project structure, async and cancellation, validation, DTOs, dependency direction, abstractions, and generated code.
- CLI generator contract (`IGenerator`, `GenerationContext`, `GenerationOptions`) that plans files independently from the command-line layer.
- Project templates: `minimal-api` (default), `mvc`, `layered`, `hexagonal`, `modular`, and `console`, with `webapi`/`web-api` retained as `minimal-api` aliases.
- `layered` and `hexagonal` templates that generate separate domain, application, infrastructure, and web projects with real project references and a solution.
- Template verification script that generates and builds every template, wired into CI.
- `laranet make:crud <Entity>` with `simple` (default), `standard`, `clean`, and `modular` profiles.
- CRUD field definitions through `--fields "Name:string,Price:decimal"`, including nullable `?` types and DataAnnotations validation in request DTOs.
- `DbContext` discovery from the nearest project, with `--context` for explicit selection.
- CRUD profile verification script that builds every profile and a flat layout in scratch EF Core web projects with warnings treated as errors, wired into CI.

### Changed

- CI now runs the full SQLite suite and the provider compatibility suite for SQL Server, PostgreSQL, MySQL, and MariaDB.
- The provider compatibility suite covers auditing and soft delete, including `IgnoreQueryFilters()` and query-filter composition.
- Provider compatibility documentation now includes MySQL, MariaDB, and verified `DateTimeOffset` storage differences.
- Testing documentation explains how to run provider compatibility tests locally, including MySQL and MariaDB.
- `Laranet.EntityFrameworkCore` package metadata now describes the optional conventions and includes their tags.
- Feedback intake now uses a Question issue template instead of a GitHub Discussions link that was unavailable.
- CLI generators now compose through a registry and share conflict, `--force`, and `--dry-run` handling in one writer.
- `laranet new` now defaults to the `minimal-api` template; the interactive prompt lists every template.
- Multi-project templates always create a solution and place projects under `src/`; `--with-*` package flags apply to the web host.
- `make:crud` uses controllers for every profile so one host configuration (`AddControllers`/`MapControllers`) works across all profiles.
- Generated `DbContext` references use `global::` so they remain valid from namespaces that share a prefix with the context namespace.

### Security

- Pinned `System.Security.Cryptography.Pkcs` to 9.0.22 in the test project to replace the vulnerable 9.0.11 pulled by `Microsoft.Data.SqlClient` 6.1.7.

## [0.1.1] - 2026-11-12

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
