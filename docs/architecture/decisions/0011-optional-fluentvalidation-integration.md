# ADR-0011: Add an Optional FluentValidation Integration

- **Status:** Accepted
- **Date:** 2026-11-14

## Context and Problem Statement

ASP.NET Core already provides validation for MVC controllers through `[ApiController]`
and DataAnnotations, and .NET 10 adds validation services for Minimal APIs. Those
mechanisms cover most request models and require no additional package.

Some applications, however, already use FluentValidation for rules that attributes
cannot express well: cross-property rules, conditional rules, asynchronous checks, and
reusable validators. In a Minimal API, connecting FluentValidation still requires a
custom endpoint filter. Every project writes the same filter, and the error response
and cancellation behavior differ from project to project.

The project needs to decide whether Laranet should ship that adapter, where it belongs,
and which guarantees it must preserve.

## Decision Drivers

- Native ASP.NET Core validation remains the default.
- FluentValidation stays an independent, optional library.
- Validation failures use the native `ValidationProblemDetails` shape.
- No custom validation engine, `FormRequest`, or `Result<T>`.
- No dependency from the adapter to unrelated Laranet packages.
- Small, predictable public surface.
- Async validation and cancellation.
- Testability without a real network.
- Minimal, justified dependencies.

## Considered Options

### Option 1 — No package; document a custom filter

Document how to write an `IEndpointFilter` around FluentValidation and leave the
implementation to each application.

- Advantage: no new package and no new public contract.
- Risk: every application repeats the same filter with inconsistent error and
  cancellation behavior.

### Option 2 — Build a validation engine or a `FormRequest` abstraction

Provide request base classes, a validation pipeline, and a custom result or exception
model inspired by other frameworks.

- Advantage: a single, highly integrated experience.
- Risk: competes with ASP.NET Core and FluentValidation, adds a large compatibility
  surface, and violates the project's stance against unnecessary abstractions.

### Option 3 — Thin endpoint filter adapter

Provide one generic endpoint filter and one extension method that delegate validation
to FluentValidation and the response to `Results.ValidationProblem`.

- Advantage: small, explicit, close to the platform, and independently removable.
- Risk: another package to version and document, and users may mistake it for a
  replacement for native validation.

### Option 4 — Put the adapter in `Laranet.AspNetCore`

Add the filter to the existing ASP.NET Core integration package.

- Advantage: one fewer package.
- Risk: forces a FluentValidation dependency on applications that only need pagination
  helpers, and couples two unrelated concerns in one release surface.

## Decision Outcome

Chosen option: **Option 3 — a thin endpoint filter adapter in a new optional package,
`Laranet.AspNetCore.Validation`.**

The work is scheduled as Sprint 18.3 in [Version 3](../../roadmap/v3.md).

### Package boundary

- Add a new optional package, `Laranet.AspNetCore.Validation`.
- Do not change `Laranet` core, `Laranet.AspNetCore`, or any existing package.
- The package references `FluentValidation` and the ASP.NET Core shared framework.
- It must not reference `Laranet.AspNetCore`, `Laranet.Data`, EF Core, or any other
  Laranet package.
- It is independently consumable. Installing it without calling the filter leaves
  native ASP.NET Core validation unchanged.
- The name is `Laranet.AspNetCore.Validation`, not `Laranet.Validation`, because the
  adapter is specific to ASP.NET Core Minimal APIs. There is no provider-neutral
  validation logic that would justify a broader package.

### Public contract

- `FluentValidationEndpointFilter<TRequest>` is a sealed `IEndpointFilter`.
- `WithFluentValidation<TRequest>` is an extension on `RouteHandlerBuilder` that adds
  the filter and returns the same builder.
- Validators are ordinary FluentValidation `IValidator<TRequest>` registrations. The
  package does not register them and does not require a specific lifetime.
- The filter is applied per endpoint. There is no global middleware and no automatic
  discovery.

### Behavior

- The first argument of type `TRequest` is validated, including `[AsParameters]`
  models.
- When no argument of type `TRequest` is present, the filter passes the request
  through.
- Every registered validator runs concurrently and receives
  `HttpContext.RequestAborted`.
- Results are combined into a `ValidationResult`; errors are grouped by property.
- A valid request calls the next delegate. An invalid request short-circuits with
  `Results.ValidationProblem(...)`, which produces the native
  `ValidationProblemDetails` response with status `400`.
- No validator registered for `TRequest`: the constructor throws
  `InvalidOperationException` with a clear message.

### MVC and native validation

- MVC is out of scope. Controllers keep `[ApiController]` and DataAnnotations
  validation. Laranet does not ship an MVC filter.
- DataAnnotations and `AddValidation()` remain the documented default for Minimal APIs.
- The package does not mix validation systems. Users pick one system per request type.

### Optional assembly scanning

`FluentValidation.DependencyInjectionExtensions` is not referenced by the package. It
is documented as an optional addition for applications that want assembly scanning.

### What Laranet will not provide

- A validation engine, `FormRequest`, `Result<T>`, or custom exception hierarchy.
- An `AddLaranetValidation` registration wrapper.
- A global validation middleware or automatic endpoint registration.
- An MVC filter.
- A replacement for ASP.NET Core validation or FluentValidation.

## Consequences

### Positive Consequences

- Applications that already use FluentValidation get one documented adapter instead of
  repeating an endpoint filter.
- The error response matches native ASP.NET Core validation.
- The package stays small, testable, and independently removable.
- Applications that do not use FluentValidation take no new dependency.

### Negative Consequences

- Another package must be versioned, tested, and documented.
- FluentValidation becomes a dependency of the adapter package.
- Users may expect broader MVC support or a validation engine and must be directed to
  the native mechanisms.
- Two validation styles coexist across the ecosystem; documentation must be explicit
  that each request type chooses one.

## Confirmation

This decision will be validated by:

- Unit tests for the missing-validator contract.
- Integration tests for valid requests, invalid requests, the
  `ValidationProblemDetails` shape, multiple validators, grouped errors by property,
  and opt-in behavior.
- Package verification that the generated `.nuspec` depends on FluentValidation 12.x,
  references the ASP.NET Core shared framework, and does not depend on other Laranet
  packages.
- Documentation covering usage, the native-first guidance, MVC limitations, and opting
  out.

## Out of Scope

- MVC and Razor Pages validation.
- A custom validation engine or attribute system.
- A request base class or a `FormRequest` equivalent.
- A `Result<T>` or custom exception hierarchy.
- Automatic validator registration or endpoint discovery.
- Validation of non-HTTP entry points.

## Related Decisions

- [ADR-0002: Use Optional Framework Integrations](0002-optional-integrations.md)
- [ADR-0005: Keep the Core Package Intentionally Empty](0005-intentionally-empty-core.md)
- [ADR-0006: Position Laranet as a Modular Toolkit with Framework-like Tooling](0006-toolkit-with-framework-tooling.md)
- [ADR-0008: Keep Runtime and Tooling Packages on One Version Train by Default](0008-runtime-and-tooling-versioning.md)
- [Module admission gates](../ecosystem.md#9-module-admission-gates)
- [Version 3 roadmap](../../roadmap/v3.md)
