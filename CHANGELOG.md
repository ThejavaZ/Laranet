# Changelog

All notable changes to Laranet will be documented in this file.

This project follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Changes that are planned or currently under development.

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

### Changed

- Aligned the `Laranet.Data` package version with the core package at `0.1.0`.
- Added NuGet metadata to `Laranet.Data`: description, authors, MIT license, repository, tags, and README.
- Expanded XML documentation for provider-dependent search, sorting, and pagination behavior.
- CI now packs and validates four production packages, including `.nuspec` dependencies, metadata, version consistency, and module-specific framework restrictions (EF Core only in `Laranet.EntityFrameworkCore`, ASP.NET Core only in `Laranet.AspNetCore`).
- Expanded the `Laranet.Data` README with sorting, allowlist, conditional filtering, composition, and provider guidance.
- Centralized the SQLite integration test infrastructure in shared test support types.

### Deprecated

### Removed

### Fixed

- Removed Entity Framework Core from the production dependency graph; it is now referenced only by the test project.
- `Search` now sends its term as a provider parameter instead of embedding it as a SQL literal.
- The pagination integration test now exercises `Paginate` and verifies its count and paged queries.
- Corrected the documented meaning of `PagedResult<T>.PageSize` to reflect the effective page size.
- `Paginate` now executes only the count query when the collection is empty or the requested page is out of range, while preserving pagination metadata.

### Security

---

## Versioning

Laranet follows Semantic Versioning:

`MAJOR.MINOR.PATCH`

- **MAJOR:** Incompatible public API changes.
- **MINOR:** New functionality introduced in a backward-compatible manner.
- **PATCH:** Backward-compatible bug fixes.

During initial development, the project may use `0.x` versions. Public APIs should be considered subject to change until the first stable release.

## Release History

No stable releases have been published yet.
