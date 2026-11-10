# Packages

Laranet is distributed as independent NuGet packages. Install only the modules an
application uses.

| Package | Responsibility | Install | Reference |
| --- | --- | --- | --- |
| `Laranet.Data` | Provider-neutral query search, sorting, filtering, and pagination | `dotnet add package Laranet.Data` | [README](../../src/Laranet.Data/README.md) |
| `Laranet.EntityFrameworkCore` | Asynchronous pagination through EF Core | `dotnet add package Laranet.EntityFrameworkCore` | [README](../../src/Laranet.EntityFrameworkCore/README.md) |
| `Laranet.AspNetCore` | Pagination HTTP headers and Minimal API results | `dotnet add package Laranet.AspNetCore` | [README](../../src/Laranet.AspNetCore/README.md) |
| `Laranet.Cli` | Project creation and code generation | `dotnet tool install --global Laranet.Cli` | [README](../../src/Laranet.Cli/README.md) |
| `Laranet` | Core package reserved for shared abstractions | `dotnet add package Laranet` | [README](../../src/Laranet/README.md) |

## Dependency boundaries

```text
Laranet.AspNetCore ──► Laranet.Data ◄── Laranet.EntityFrameworkCore
        │                                      │
        └──────── optional integrations ───────┘
                        (no mandatory dependency on Core)
```

- `Laranet.Data` does not reference EF Core, ASP.NET Core, or any database provider.
- `Laranet.EntityFrameworkCore` references `Laranet.Data` and EF Core.
- `Laranet.AspNetCore` references `Laranet.Data` and the ASP.NET Core shared
  framework.
- `Laranet.Cli` is a .NET tool with no runtime dependency on the other packages.
- `Laranet` remains small and is not required by the other packages.

## Which package do I need?

| Scenario | Packages |
| --- | --- |
| Query an existing `IQueryable<T>` | `Laranet.Data` |
| Asynchronous EF Core pagination | `Laranet.EntityFrameworkCore` |
| Paginated Minimal API or MVC response | `Laranet.AspNetCore` + `Laranet.EntityFrameworkCore` |
| Scaffold a new project or generate code | `Laranet.Cli` |

## Examples

- [Console sample](../../samples/Laranet.Samples.Console) uses `Laranet.Data` over
  LINQ to Objects.
- [Web API sample](../../samples/Laranet.Samples.WebApi) combines all three runtime
  packages with EF Core and SQLite.
- [Quick start](../getting-started/quick-start.md)
- [First application](../getting-started/first-application.md)

## API reference

- [Core (`Laranet`)](../api/core.md)
- [Data (`Laranet.Data`)](../api/data.md)
- [Integrations](../api/integrations.md)

## Installation details

See [Installation](../getting-started/installation.md) for prerequisites, local
package sources, and the from-source workflow.

## Compatibility

Supported .NET versions, integration versions, and the provider matrix are documented
under [Compatibility](../compatibility/support-policy.md).
