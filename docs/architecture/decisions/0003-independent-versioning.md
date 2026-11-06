# ADR-0003: Maintain Independent Project Versioning

- **Status:** Accepted
- **Date:** 2026-11-05

## Context and Problem Statement

Laranet is built for the .NET ecosystem and will evolve alongside the platform.

.NET follows its own release schedule and versioning conventions. Laranet, however, has its own public APIs, packages, release lifecycle, and compatibility requirements.

Automatically matching Laranet release numbers to .NET versions would unnecessarily couple two independent release cycles.

How should Laranet version its packages while maintaining compatibility with supported .NET versions?

## Decision Drivers

- Independent release cycles.
- Predictable package upgrades.
- Clear communication of breaking changes.
- Compatibility with supported .NET versions.
- Consistent package versioning.
- Long-term API stability.

## Considered Options

- Match Laranet versions directly to .NET versions.
- Use independent Semantic Versioning.
- Use an arbitrary versioning scheme without formal compatibility rules.

## Decision Outcome

Chosen option: **Use independent Semantic Versioning (SemVer 2.0.0).**

Laranet will maintain its own version numbers independently of .NET release numbers.

The version format will be:

`MAJOR.MINOR.PATCH`

Version increments follow these rules:

- **MAJOR:** Introduces incompatible public API changes.
- **MINOR:** Introduces new functionality in a backward-compatible manner.
- **PATCH:** Introduces backward-compatible bug fixes.

During initial development, Laranet will use `0.x` versions.

The initial public release is planned as `0.1.0`.

Before the first stable `1.0.0` release, public APIs may evolve with fewer compatibility guarantees.

All distributed packages belonging to the same Laranet release should use consistent version numbers unless a future architectural decision establishes a justified exception.

.NET target frameworks and supported runtime versions will be documented separately through the compatibility policy.

## Consequences

### Positive Consequences

- Laranet controls its own release lifecycle.
- Package versions communicate the nature of changes.
- Breaking changes can be identified through major version increments.
- .NET upgrades do not automatically require Laranet version changes.
- Different .NET target frameworks can be supported by the same Laranet release when technically compatible.

### Negative Consequences

- Laranet must maintain its own release and compatibility policy.
- Compatibility with supported .NET versions requires explicit testing.
- Release management must be coordinated across packages.
- Breaking changes require migration documentation.

## Confirmation

This decision will be validated through:

- Consistent package versioning.
- Release notes and changelog updates.
- Compatibility testing against supported target frameworks.
- Documented migration procedures for breaking changes.

## Related Decisions

- [Versioning Policy](../../compatibility/versioning.md)
- [Support Policy](../../compatibility/support-policy.md)
- [Architecture Overview](../overview.md)
