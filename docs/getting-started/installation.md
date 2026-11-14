# Installation

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git, if you want to build Laranet from source

Laranet targets `net10.0`. Applications must target .NET 10 or a compatible later
version when one becomes supported.

## Packages

Each Laranet module is distributed as an independent package. Install only the ones
your application uses.

| Package | Purpose | Install |
| --- | --- | --- |
| `Laranet.Data` | Provider-neutral `IQueryable` search, sorting, filtering, and pagination | `dotnet add package Laranet.Data` |
| `Laranet.EntityFrameworkCore` | Asynchronous pagination and optional conventions through EF Core | `dotnet add package Laranet.EntityFrameworkCore` |
| `Laranet.AspNetCore` | Pagination HTTP headers and Minimal API results | `dotnet add package Laranet.AspNetCore` |
| `Laranet` | Core package reserved for shared abstractions | `dotnet add package Laranet` |
| `Laranet.Cli` | Project creation and code generation tool | See below |
| `Laranet.Http` | Typed, cancellation-aware convenience layer over `HttpClient` | Next release; source only for now |

`Laranet.EntityFrameworkCore` and `Laranet.AspNetCore` reference `Laranet.Data`, so
you do not need to install `Laranet.Data` separately when you use them.
`Laranet.Http` is implemented in the repository and will be published with the next
release.

## Install the CLI

Install the CLI as a global .NET tool:

```bash
dotnet tool install --global Laranet.Cli
```

Verify the installation:

```bash
laranet --version
```

To pin a specific version to a repository, use a local tool manifest instead:

```bash
dotnet new tool-manifest
dotnet tool install --local Laranet.Cli
dotnet tool run laranet --version
```

## Build from source

To install the packages from a local build instead of NuGet.org:

```bash
git clone https://github.com/TheJavaZ/Laranet.git
cd Laranet
dotnet build Laranet.slnx --configuration Release
```

Pack the packages you need:

```bash
mkdir -p artifacts/packages
dotnet pack src/Laranet.Data/Laranet.Data.csproj --configuration Release --no-build --output artifacts/packages
dotnet pack src/Laranet.EntityFrameworkCore/Laranet.EntityFrameworkCore.csproj --configuration Release --no-build --output artifacts/packages
dotnet pack src/Laranet.AspNetCore/Laranet.AspNetCore.csproj --configuration Release --no-build --output artifacts/packages
dotnet pack src/Laranet.Http/Laranet.Http.csproj --configuration Release --no-build --output artifacts/packages
dotnet pack src/Laranet.Cli/Laranet.Cli.csproj --configuration Release --no-build --output artifacts/packages
```

Register the local folder as a NuGet source:

```bash
dotnet nuget add source "$(pwd)/artifacts/packages" --name laranet-local
```

Then install packages with the version produced by the build:

```bash
dotnet add package Laranet.Data --version 0.1.1
```

Install the locally built CLI:

```bash
dotnet tool install --global Laranet.Cli --version 0.1.1 --add-source artifacts/packages
```

Alternatively, add project references directly to `src/` while working inside the
repository. The projects in [`samples/`](../../samples) use this approach.

## Documentation status

Laranet is in initial development. The package identifiers and installation commands
above describe the `0.1.1` release. If a package is not available from NuGet yet, use
the from-source workflow.

## Next steps

- [Quick start](quick-start.md)
- [First application](first-application.md)
- [Package overview](../packages/README.md)
