# CLI

`Laranet.Cli` creates projects and generates editable C# code. The complete command
reference is the [CLI package README](../../src/Laranet.Cli/README.md). This guide
covers the workflow.

## Install

```bash
dotnet tool install --global Laranet.Cli
laranet --version
```

Or pin it to a repository:

```bash
dotnet new tool-manifest
dotnet tool install --local Laranet.Cli
```

## Create a project

```bash
laranet new my-api
```

Choose a template and a layout:

```bash
laranet new my-api --layout repository
laranet new my-web --template mvc
laranet new my-service --template layered
laranet new my-tool --template console --layout project
```

| Template | Structure |
| --- | --- |
| `minimal-api` (default) | One Web SDK project with endpoints in `Program.cs`. |
| `mvc` | One Web SDK project with controllers and Razor views. |
| `layered` | Domain, application, infrastructure, and web projects. |
| `hexagonal` | The same projects, with ports and adapters. |
| `modular` | One Web SDK project with feature folders. |
| `console` | One executable project without ASP.NET Core. |

| Layout | Structure |
| --- | --- |
| `project` (default) | Project files directly inside `<name>/`. |
| `repository` | Project under `<name>/src/<name>/` plus solution, README, and `.gitignore` at the root. |

Add `--sln` to the `project` layout to create a solution. The `repository` layout
always creates one. `layered` and `hexagonal` always use the repository shape
because they need a solution to build their projects together.

Reference optional packages during creation:

```bash
laranet new shop-api --with-data --with-efcore --with-aspnetcore
```

## Generate code

```bash
cd src/MyApp
laranet make:model Product
laranet make:service Product
laranet make:controller Product
laranet make:middleware RequestLogging
laranet make:resource Product
laranet make:crud Product --fields "Name:string,Price:decimal"
```

| Command | Output |
| --- | --- |
| `make:model <name>` | `Models/<Name>.cs` |
| `make:service <name>` | `Services/<Name>Service.cs` |
| `make:controller <name>` | `Controllers/<Name>Controller.cs` |
| `make:middleware <name>` | `Middleware/<Name>Middleware.cs` |
| `make:resource <name>` | Model, service, and controller |
| `make:crud <entity>` | Entity, DTOs, and CRUD endpoints |
| `make:repository <name>` | Optional `Repositories/<Name>Repository.cs` scaffold |

Names are normalized to PascalCase and suffixes are applied only when missing.

## Generate CRUD

`make:crud` creates an entity and a controller-based CRUD surface. The target
project must reference Entity Framework Core and contain a `DbContext`. The CLI
discovers the only context in the nearest project, or accepts one with
`--context`:

```bash
laranet make:crud Product --context Shop.Data.ShopDbContext
```

Fields are `Name:type` pairs. The `?` suffix makes a field nullable, and the
default is `Name:string`:

```bash
laranet make:crud Product \
  --fields "Name:string,Price:decimal,Description:string?,ReleasedOn:DateTime?"
```

Supported types: `string`, `int`, `long`, `decimal`, `double`, `float`, `bool`,
`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, and `Guid`. `Id` is generated
automatically.

Choose a profile with `--profile`:

| Profile | Structure |
| --- | --- |
| `simple` (default) | Entity, DTOs, and a controller that uses `DbContext` directly. |
| `standard` | Adds a service and a registration extension. |
| `clean` | Adds domain, application, and infrastructure folders with a repository interface and implementation. |
| `modular` | Places the feature under `Features/<PluralEntity>/` with the `standard` structure. |

The `clean` profile creates folders inside the current project; it does not create
separate projects. The `standard`, `clean`, and `modular` profiles generate an
`Add<Entity>Crud()` extension that you call from the host.

`make:crud` does not modify the `DbContext`. Map the entity yourself:

```csharp
public DbSet<Product> Products => Set<Product>();
```

All profiles generate controllers, so register them with `AddControllers()` and
`MapControllers()`. See the
[CLI package README](../../src/Laranet.Cli/README.md#generate-crud) for the full
file list of each profile.

## Namespaces

Without `--namespace`, the CLI walks up to the nearest `.csproj` and uses
`RootNamespace`, `AssemblyName`, or the project file name. Convention folders are
appended to that base namespace.

Pass `--namespace` when no project file exists:

```bash
laranet make:model Product --namespace Shop.Api
```

## Custom templates

Override built-in generators through `.laranet/templates`:

```text
.laranet/
└── templates/
    ├── model.tpl
    ├── service.tpl
    ├── controller.tpl
    ├── middleware.tpl
    └── repository.tpl
```

Templates support `{{Name}}` and `{{Namespace}}`. The nearest `.laranet/templates`
directory wins. Folder, file name, and namespace remain convention-based.

Example `.laranet/templates/model.tpl`:

```csharp
namespace {{Namespace}};

public sealed record {{Name}}(int Id, DateTimeOffset CreatedAt);
```

## Safe generation

- Existing files are never overwritten without `--force`.
- Conflicts are reported before writing, so a failed command does not leave a
  partial result.
- `--dry-run` shows the planned files.
- `--flat` places files directly in the output directory.
- `--interactive` (`-i`) prompts for missing values and overwrite confirmation.
- Without `--interactive`, commands never read from standard input.

## What the generator does not do

- It does not add runtime metadata or special attributes.
- It does not require a repository layer.
- `make:crud` does not modify your `DbContext` or `Program.cs`; map the entity and
  register controllers yourself.
- It does not modify existing application code.
- It does not replace `dotnet new` for general project scaffolding.

The generated code is normal C# that you own and can edit.

## Next steps

- [CLI package README](../../src/Laranet.Cli/README.md)
- [First application](../getting-started/first-application.md)
