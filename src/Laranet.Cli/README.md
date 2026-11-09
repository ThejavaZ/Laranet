# Laranet.Cli

Command-line project creation and code generation for Laranet applications.

The CLI generates normal, editable C# files. It does not add runtime metadata,
attributes, or hidden configuration to your project.

## Installation

Install it as a global .NET tool:

```bash
dotnet tool install --global Laranet.Cli
```

Or pin it to a repository with a local tool manifest:

```bash
dotnet new tool-manifest
dotnet tool install --local Laranet.Cli
```

## Create a project

```bash
laranet new my-api
```

This creates a Web API project in `./my-api`:

```text
my-api/
├── .gitignore
├── appsettings.Development.json
├── appsettings.json
├── my-api.csproj
├── Program.cs
├── Properties/launchSettings.json
└── README.md
```

Then run it:

```bash
cd my-api
dotnet run
```

Choose another template:

```bash
laranet new my-console --template console
```

The `console` template creates a single executable project with no ASP.NET Core
files:

```text
my-console/
├── .gitignore
├── my-console.csproj
├── Program.cs
└── README.md
```

### Project options

| Option | Description |
| --- | --- |
| `-o`, `--output <dir>` | Directory where the project folder is created. Defaults to the current directory. |
| `-n`, `--namespace <ns>` | Root namespace. Derived from the project name by default. |
| `-t`, `--template <name>` | `webapi` (default) or `console`. |
| `--with-data` | References the `Laranet.Data` package. |
| `--with-efcore` | References the `Laranet.EntityFrameworkCore` package. |
| `--with-aspnetcore` | References the `Laranet.AspNetCore` package. |
| `-f`, `--force` | Writes into an existing non-empty directory and overwrites generated files. |
| `--dry-run` | Shows the files that would be generated without writing them. |

The project folder keeps the name you pass, while the root namespace is
normalized to PascalCase: `laranet new my-api` creates the `my-api` folder with
the `MyApi` namespace.

Requested package references use the same version as the CLI.

## Commands

| Command | Generates |
| --- | --- |
| `laranet new <name>` | A new project |
| `laranet make:model <name>` | A model class in `Models/` |
| `laranet make:service <name>` | A service class in `Services/` |
| `laranet make:controller <name>` | An ASP.NET Core controller in `Controllers/` |
| `laranet make:resource <name>` | A model, a service, and a controller together |

Names are normalized to PascalCase. For example, `product-category`,
`product_category`, and `productCategory` all generate `ProductCategory`.

Suffixes are applied only when missing: `make:service Product` creates
`ProductService`, while `make:service ProductService` does not create
`ProductServiceService`.

## Examples

Generate a model inside an existing project:

```bash
cd src/MyApp
laranet make:model Product
```

This creates `src/MyApp/Models/Product.cs`:

```csharp
namespace MyApp.Models;

public class Product
{
    public int Id { get; set; }
}
```

Generate an API controller:

```bash
laranet make:controller Product
```

This creates `Controllers/ProductController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductController : ControllerBase
{
}
```

Generate a complete resource:

```bash
laranet make:resource Product --output src/MyApp
```

```text
src/MyApp/
├── Models/Product.cs
├── Services/ProductService.cs
└── Controllers/ProductController.cs
```

## Generation options

| Option | Description |
| --- | --- |
| `-o`, `--output <dir>` | Output directory. Defaults to the current directory. |
| `-n`, `--namespace <ns>` | Namespace for generated code. Inferred by default. |
| `-f`, `--force` | Overwrites existing files. |
| `--dry-run` | Shows the files that would be generated without writing them. |
| `--flat` | Places files directly in the output directory instead of convention folders. |

## Namespace inference

When `--namespace` is omitted, the CLI walks up from the output directory to the
nearest `.csproj` file and uses:

1. `RootNamespace`, when present.
2. `AssemblyName`, when present.
3. The project file name.

Convention folders are appended to the base namespace:

| Command | Namespace |
| --- | --- |
| `make:model Product` | `<base>.Models` |
| `make:service Product` | `<base>.Services` |
| `make:controller Product` | `<base>.Controllers` |

With `--flat`, files use the base namespace directly.

If no project file can be found, pass `--namespace` explicitly.

## Overwrite protection

Existing files are never overwritten unless `--force` is specified. When a
command generates several files, all conflicts are reported before anything is
written, so a failed command does not leave a partial result.

`laranet new` also refuses to write into a directory that already contains
files, unless `--force` is specified. Unrelated files in that directory are
never deleted.

Use `--dry-run` to inspect the planned files first:

```bash
laranet make:resource Product --dry-run
```

## Requirements

- .NET 10 SDK

## License

Laranet is licensed under the [MIT License](https://github.com/TheJavaZ/Laranet/blob/main/LICENSE).
