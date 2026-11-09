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
| `-i`, `--interactive` | Prompts for missing values and overwrite confirmation. |

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
| `laranet make:middleware <name>` | An ASP.NET Core middleware in `Middleware/` |
| `laranet make:resource <name>` | A model, a service, and a controller together |
| `laranet make:repository <name>` | An optional repository scaffold in `Repositories/` |

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

Generate a middleware:

```bash
laranet make:middleware RequestLogging
```

This creates `Middleware/RequestLoggingMiddleware.cs` with a standard
`RequestDelegate` pipeline component. Register it with
`app.UseMiddleware<RequestLoggingMiddleware>()`.

The repository generator is optional and intentionally minimal. Laranet does
not assume a repository layer:

```bash
laranet make:repository Product
```

This creates an empty `Repositories/ProductRepository.cs` scaffold. Complete it
only when the abstraction solves a real problem in your application. It is never
included in `make:resource`.

## Generation options

| Option | Description |
| --- | --- |
| `-o`, `--output <dir>` | Output directory. Defaults to the current directory. |
| `-n`, `--namespace <ns>` | Namespace for generated code. Inferred by default. |
| `-f`, `--force` | Overwrites existing files. |
| `--dry-run` | Shows the files that would be generated without writing them. |
| `--flat` | Places files directly in the output directory instead of convention folders. |
| `-i`, `--interactive` | Prompts for missing values and overwrite confirmation. |

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
| `make:middleware Product` | `<base>.Middleware` |
| `make:repository Product` | `<base>.Repositories` |

With `--flat`, files use the base namespace directly.

If no project file can be found, pass `--namespace` explicitly.

## Customizing templates

The built-in templates can be overridden per repository. Create a
`.laranet/templates` directory in your project or repository root:

```text
.laranet/
└── templates/
    ├── model.tpl
    ├── service.tpl
    ├── controller.tpl
    ├── middleware.tpl
    └── repository.tpl
```

Templates are discovered by walking up from the output directory, so a single
directory at the repository root applies to every project inside it. When several
`.laranet/templates` directories exist, the nearest one wins.

Only the matching file is replaced. Folder, file name, and namespace remain
convention-based.

Templates support two placeholders:

| Placeholder | Replaced with |
| --- | --- |
| `{{Name}}` | The normalized type name, including its suffix |
| `{{Namespace}}` | The resolved namespace for the generated file |

Other `{{...}}` placeholders are left untouched.

For example, `.laranet/templates/model.tpl`:

```csharp
namespace {{Namespace}};

public sealed record {{Name}}(int Id, DateTime CreatedAt);
```

`laranet make:model Product` then generates `Models/Product.cs` with that
content. Empty template files are rejected with an error.

Project templates created by `laranet new` are built in and are not customized
through this mechanism yet.

## Interactive mode

Add `--interactive` (`-i`) to `new` or any `make` command to be prompted for
missing values and overwrite confirmations:

```text
$ laranet new --interactive
Project name: shop-api
Template [webapi/console] (webapi): webapi
Reference Laranet.Data? [y/N] n
Reference Laranet.EntityFrameworkCore? [y/N] y
Reference Laranet.AspNetCore? [y/N] n
```

```text
$ laranet make:model --interactive
Name: Product
Namespace: Shop.Api
```

Behavior:

- Values provided on the command line are never asked again.
- Supplying any `--with-*` flag skips the package questions and uses exactly the
  requested packages.
- In `make` commands, the namespace is only requested when it cannot be inferred
  from a project file.
- Invalid names and namespaces are reported and re-prompted.
- Existing files prompt once for confirmation before anything is written.
- Without `--interactive`, commands never read from standard input.

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
