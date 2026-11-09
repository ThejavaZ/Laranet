# Laranet

**A Laravel-inspired toolkit for .NET.**

Laranet brings expressive APIs, developer-friendly abstractions, and elegant patterns
to the C# ecosystem without replacing the native .NET building blocks.

This package is the foundation of the Laranet toolkit. It is intentionally small and
contains no public API yet. Functionality is distributed in independent, optional
packages:

| Package | Purpose |
| --- | --- |
| [`Laranet.Data`](https://www.nuget.org/packages/Laranet.Data) | Provider-neutral `IQueryable` search, sorting, filtering, and pagination |
| [`Laranet.EntityFrameworkCore`](https://www.nuget.org/packages/Laranet.EntityFrameworkCore) | Asynchronous pagination through Entity Framework Core |
| [`Laranet.AspNetCore`](https://www.nuget.org/packages/Laranet.AspNetCore) | Pagination HTTP headers and Minimal API results |
| [`Laranet.Cli`](https://www.nuget.org/packages/Laranet.Cli) | Project creation and code generation tool |

## Install

Install only the packages an application uses:

```bash
dotnet add package Laranet.Data
dotnet add package Laranet.EntityFrameworkCore
dotnet add package Laranet.AspNetCore
```

## Philosophy

- **Native .NET first:** use the platform instead of reinventing it.
- **Optional integrations:** Core stays independent of frameworks and providers.
- **No unnecessary abstractions:** every abstraction must solve a demonstrated problem.
- **Editable generated code:** the CLI produces ordinary C#.
- **Provider neutrality:** data utilities leave execution to the underlying provider.

## Documentation

- [Documentation](https://github.com/TheJavaZ/Laranet/blob/main/docs/README.md)
- [Installation](https://github.com/TheJavaZ/Laranet/blob/main/docs/getting-started/installation.md)
- [Quick start](https://github.com/TheJavaZ/Laranet/blob/main/docs/getting-started/quick-start.md)
- [Package overview](https://github.com/TheJavaZ/Laranet/blob/main/docs/packages/README.md)

## Requirements

- .NET 10

## License

Laranet is licensed under the [MIT License](https://github.com/TheJavaZ/Laranet/blob/main/LICENSE).
