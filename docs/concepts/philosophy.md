# Project Philosophy

Laranet is a Laravel-inspired toolkit for .NET.

The inspiration is about developer experience, not about copying Laravel's
architecture. The guiding principle is:

> Don't make .NET look like Laravel. Make .NET feel better to work with.

## A toolkit with framework-like tooling

Laranet is a modular toolkit, not an opinionated application framework with its own
host and lifecycle. It does not prescribe a folder structure or require a base class.
Its tooling — conventions, templates, and code generation — can feel framework-like
while the runtime stays optional. See
[ADR-0006](../architecture/decisions/0006-toolkit-with-framework-tooling.md).

Applications keep using the native .NET building blocks they already know:

- `Microsoft.Extensions.DependencyInjection`
- LINQ and `IQueryable`
- Entity Framework Core
- ASP.NET Core Minimal APIs and MVC
- The standard configuration and logging infrastructure

Laranet adds focused utilities on top of those foundations.

## Native .NET first

Before adding an abstraction, the project checks whether .NET already solves the
problem. If the platform provides a good solution, Laranet uses it instead of
wrapping it.

This is why the toolkit does not introduce:

- A custom dependency injection container.
- A replacement for ASP.NET Core model binding or validation.
- A generic result wrapper for application errors.
- A repository or unit-of-work layer by default.

## Optional integrations

The core stays independent of application frameworks and database providers.
Framework-specific behavior lives in optional packages that can be installed only
when needed.

An application that uses `Laranet.Data` does not inherit ASP.NET Core or EF Core.
An application that uses EF Core does not have to use ASP.NET Core. Integrations may
depend on shared modules, but never the other way around.

## No unnecessary abstractions

An abstraction must solve a demonstrated problem. The project avoids introducing
patterns only because they are popular.

In practice:

- Prefer composition over inheritance.
- Do not create interfaces to wrap every class.
- Do not add configuration options without a real use case.
- Do not hide expensive operations behind innocent-looking methods.
- Do not switch an `IQueryable` to in-memory execution silently.

## Provider neutrality

`Laranet.Data` builds standard `Queryable` expression trees and delegates execution
to the underlying provider. It does not assume that every provider behaves like
Entity Framework Core.

Provider-specific execution, such as asynchronous pagination, belongs in the
corresponding integration package.

## Editable generated code

The CLI generates ordinary C# that you own and can edit. Generated projects and
types do not require runtime metadata, special attributes, or hidden configuration.

Templates can be overridden, but nothing forces an application to keep using the
generator after the first generation.

## Documentation is part of the product

Public APIs need clear behavior contracts and examples. Documentation must describe
what exists, not what is planned. Behavior that depends on a provider or database
must be documented as such.

## Evolution through real usage

New modules, generators, and abstractions should emerge from concrete requirements.
Feedback from real applications takes precedence over speculative design.

See the [roadmap](../roadmap/README.md) for the current scope and the work deferred
to later versions.
