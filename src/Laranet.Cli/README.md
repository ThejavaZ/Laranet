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

This creates a Minimal API project in `./my-api`:

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

### Project templates

| Template | Structure | Use it for |
| --- | --- | --- |
| `minimal-api` (default) | One Web SDK project; endpoints in `Program.cs`. | Small HTTP services and APIs. |
| `mvc` | One Web SDK project with `Controllers/` and Razor views in `Views/`. | Server-rendered pages and traditional web applications. |
| `layered` | `Domain`, `Application`, `Infrastructure`, and `Web` projects with real project references. | Business applications that benefit from explicit layer boundaries. |
| `hexagonal` | The same four projects, with `Ports/` in the application layer and `Adapters/` in the infrastructure layer. | Systems where ports and adapters represent a real boundary. |
| `modular` | One Web SDK project with feature folders under `Features/`. | Applications organized by feature without separate projects. |
| `console` | One executable project with no ASP.NET Core files. | Command-line tools and background utilities. |

```bash
laranet new my-web --template mvc
laranet new my-service --template layered
laranet new my-tool --template console
```

The CLI also accepts `minimal` and the older `webapi` and `web-api` names as
aliases for `minimal-api`.

The `console` template creates:

```text
my-tool/
├── .gitignore
├── my-tool.csproj
├── Program.cs
└── README.md
```

`layered` and `hexagonal` always create a solution and place their projects under
`src/`:

```text
my-service/
├── .gitignore
├── README.md
├── my-service.slnx
└── src/
    ├── my-service.Domain/
    ├── my-service.Application/
    ├── my-service.Infrastructure/
    └── my-service.Web/
```

The `Web` project is the runnable host. In multi-project templates, `--layout` and
`--sln` do not change the structure because the solution is always required, and
the `--with-*` flags add package references to the host project.

Each template keeps only what it promises:

- `minimal-api` adds no controllers, views, or layer projects.
- `mvc` uses controllers and Razor views; it is not an API-first template.
- `layered` separates the domain, application, infrastructure, and host projects,
  without adding CQRS, mediator, repositories, or unit-of-work types.
- `hexagonal` uses the same projects with explicit ports and adapters. Choose it
  only when that boundary solves a real problem; otherwise `layered` or
  `minimal-api` is simpler.
- `modular` enforces feature boundaries by convention rather than by project
  references.
- `console` has no ASP.NET Core configuration or web SDK.

### Project options

| Option | Description |
| --- | --- |
| `-o`, `--output <dir>` | Directory where the project folder is created. Defaults to the current directory. |
| `-n`, `--namespace <ns>` | Root namespace. Derived from the project name by default. |
| `-t`, `--template <name>` | `minimal-api` (default), `mvc`, `layered`, `hexagonal`, `modular`, or `console`. |
| `-l`, `--layout <name>` | `project` (default) or `repository`. Applies to single-project templates. |
| `--sln` | Creates a `<name>.slnx` solution in the `project` layout. Multi-project templates always create one. |
| `--with-data` | References the `Laranet.Data` package. |
| `--with-efcore` | References the `Laranet.EntityFrameworkCore` package. |
| `--with-aspnetcore` | References the `Laranet.AspNetCore` package. |
| `-f`, `--force` | Writes into an existing non-empty directory and overwrites generated files. |
| `--dry-run` | Shows the files that would be generated without writing them. |
| `-i`, `--interactive` | Prompts for missing values and overwrite confirmation. |

The project folder keeps the name you pass, while the root namespace is
normalized to PascalCase: `laranet new my-api` creates the `my-api` folder with
the `MyApi` namespace.

Requested package references use the same version as the CLI. In multi-project
templates they are added to the `Web` host project.

### Project layouts

`--layout project` (default) keeps everything inside the project folder:

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

`--layout repository` prepares a repository with the project under `src/` and a
solution at the root:

```text
my-api/
├── .gitignore
├── README.md
├── my-api.slnx
└── src/
    └── my-api/
        ├── appsettings.Development.json
        ├── appsettings.json
        ├── my-api.csproj
        ├── Program.cs
        └── Properties/launchSettings.json
```

The `repository` layout always creates the solution. Add `--sln` to the
`project` layout when you want one too. `layered` and `hexagonal` already use
the repository shape because they need a solution to build their projects
together.

## Commands

| Command | Generates |
| --- | --- |
| `laranet new <name>` | A new project |
| `laranet make:model <name>` | A model class in `Models/` |
| `laranet make:service <name>` | A service class in `Services/` |
| `laranet make:controller <name>` | An ASP.NET Core controller in `Controllers/` |
| `laranet make:middleware <name>` | An ASP.NET Core middleware in `Middleware/` |
| `laranet make:resource <name>` | A model, a service, and a controller together |
| `laranet make:crud <entity>` | An entity, DTOs, and CRUD endpoints |
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

## Generate CRUD

`make:crud` generates an entity and a complete controller-based CRUD surface for it:

```bash
laranet make:crud Product --fields "Name:string,Price:decimal,Stock:int"
```

The generated code uses Entity Framework Core. The target project must reference
EF Core and contain a `DbContext`. The command discovers the only `DbContext` in
the nearest project, or accepts one explicitly:

```bash
laranet make:crud Product --context Shop.Data.ShopDbContext
```

`make:crud` does not modify your `DbContext`. Map the entity before using the
endpoints:

```csharp
public DbSet<Product> Products => Set<Product>();
```

All profiles generate controllers, so the host must register and map them:

```csharp
builder.Services.AddControllers();
// ...
app.MapControllers();
```

### Fields

Fields use `Name:type` pairs separated by commas. Types are case-insensitive, and
a `?` suffix makes a field nullable:

```bash
laranet make:crud Product \
  --fields "Name:string,Price:decimal,Description:string?,ReleasedOn:DateTime?"
```

Supported types: `string`, `int`, `long`, `decimal`, `double`, `float`, `bool`,
`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, and `Guid`. The default is
`Name:string`. `Id` is always generated as `int` and cannot be declared as a field.

### Profiles

Choose the amount of structure with `--profile`:

| Profile | Structure |
| --- | --- |
| `simple` (default) | Entity, request/response DTOs, and a controller that uses `DbContext` directly. |
| `standard` | Adds a service class and a dependency-injection registration extension. |
| `clean` | Adds domain, application, and infrastructure folders with a repository interface and implementation. |
| `modular` | Places the whole feature under `Features/<PluralEntity>/` with the `standard` structure. |

```bash
laranet make:crud Product --profile standard
laranet make:crud Product --profile clean
laranet make:crud Product --profile modular
```

The `clean` profile creates folders inside the current project. It does not create
separate projects. Move the folders into separate projects when your application
needs that boundary.

### Generated files

With the default `Product` entity:

| Profile | Files |
| --- | --- |
| `simple` | `Models/Product.cs`, `Contracts/ProductRequest.cs`, `Contracts/ProductResponse.cs`, `Controllers/ProductController.cs` |
| `standard` | The `simple` files plus `Services/ProductService.cs` and `Extensions/ProductServiceCollectionExtensions.cs` |
| `clean` | `Domain/Entities/Product.cs`, `Application/Products/ProductRequest.cs`, `Application/Products/ProductResponse.cs`, `Application/Abstractions/IProductRepository.cs`, `Infrastructure/Persistence/ProductRepository.cs`, `Application/Products/ProductService.cs`, `Controllers/ProductController.cs`, `Extensions/ProductServiceCollectionExtensions.cs` |
| `modular` | `Features/Products/Product.cs`, `Features/Products/ProductRequest.cs`, `Features/Products/ProductResponse.cs`, `Features/Products/ProductService.cs`, `Features/Products/ProductController.cs`, `Features/Products/ProductServiceCollectionExtensions.cs` |

The `standard`, `clean`, and `modular` profiles generate a registration extension.
Call it from the host:

```csharp
builder.Services.AddProductCrud();
```

The `simple` profile has no registration extension because the controller receives
the `DbContext` directly.

### CRUD options

| Option | Description |
| --- | --- |
| `-p`, `--profile <name>` | `simple` (default), `standard`, `clean`, or `modular`. |
| `-c`, `--context <type>` | Fully qualified `DbContext` type. Discovered by default. |
| `--fields <list>` | Comma-separated `Name:type` pairs. Defaults to `Name:string`. |

`make:crud` also supports `--output`, `--namespace`, `--force`, `--dry-run`,
`--flat`, and `--interactive`. With `--flat`, every file goes directly into the
output directory and uses the base namespace.

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
| `make:crud Product` | Profile-specific folders such as `<base>.Models` and `<base>.Contracts` |
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
Template [minimal-api/mvc/layered/hexagonal/modular/console] (minimal-api): minimal-api
Layout [project/repository] (project): repository
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
- `make:crud` prompts for the profile when `--profile` is omitted. It prompts for
  the `DbContext` only when it cannot be discovered.
- Invalid names, namespaces, profiles, fields, and context types are reported and
  re-prompted.
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
