namespace Laranet.Cli.Generation;

/// <summary>
/// Contains the file contents used to create new projects.
/// </summary>
internal static class ProjectTemplates
{
    private const string WebSdk = "Microsoft.NET.Sdk.Web";
    private const string LibrarySdk = "Microsoft.NET.Sdk";

    private const string MinimalApiProgram = """
        var builder = WebApplication.CreateBuilder(args);

        var app = builder.Build();

        app.MapGet("/", () => Results.Ok(new { Status = "running" }));
        app.MapGet("/health", () => Results.Ok(new { Status = "healthy" }));

        app.Run();

        """;

    private const string ConsoleProgram = """
        Console.WriteLine("{{Namespace}} is ready.");

        """;

    private const string MvcProgram = """
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseAuthorization();

        app.MapGet("/health", () => Results.Ok(new { Status = "healthy" }));
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();

        """;

    private const string MvcHomeController = """
        using Microsoft.AspNetCore.Mvc;

        namespace {{Namespace}};

        public sealed class HomeController : Controller
        {
            public IActionResult Index()
            {
                return View();
            }
        }

        """;

    private const string MvcLayoutView = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>@ViewData["Title"] - {{ProjectName}}</title>
        </head>
        <body>
            <main>
                @RenderBody()
            </main>
        </body>
        </html>

        """;

    private const string MvcIndexView = """
        @{
            ViewData["Title"] = "Home";
        }

        <h1>Welcome to {{ProjectName}}</h1>
        <p>Your MVC application is running.</p>

        """;

    private const string MvcViewImports = """
        @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

        """;

    private const string MvcViewStart = """
        @{
            Layout = "_Layout";
        }

        """;

    private const string ModularProgram = """
        using {{FeaturesNamespace}};

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<CatalogService>();

        var app = builder.Build();

        app.MapGet("/", () => Results.Ok(new { Status = "running" }));
        app.MapGet("/health", () => Results.Ok(new { Status = "healthy" }));
        app.MapCatalogEndpoints();

        app.Run();

        """;

    private const string ModularProduct = """
        namespace {{Namespace}};

        public sealed record Product(Guid Id, string Name);

        """;

    private const string ModularCatalogService = """
        namespace {{Namespace}};

        public sealed class CatalogService
        {
            private readonly List<Product> _products = [];

            public IReadOnlyList<Product> All() => _products;

            public Product Add(string name)
            {
                var product = new Product(Guid.NewGuid(), name);
                _products.Add(product);
                return product;
            }
        }

        """;

    private const string ModularCatalogEndpoints = """
        namespace {{Namespace}};

        public static class CatalogEndpoints
        {
            public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
            {
                var group = endpoints.MapGroup("/catalog");

                group.MapGet("/", (CatalogService service) => Results.Ok(service.All()));
                group.MapPost("/", (CreateProduct request, CatalogService service) =>
                {
                    var product = service.Add(request.Name);
                    return Results.Created($"/catalog/{product.Id}", product);
                });

                return endpoints;
            }
        }

        public sealed record CreateProduct(string Name);

        """;

    private const string ProductEntity = """
        namespace {{Namespace}};

        public sealed record Product(Guid Id, string Name);

        """;

    private const string ProductStoreInterface = """
        using {{DomainNamespace}};

        namespace {{Namespace}};

        public interface IProductStore
        {
            IReadOnlyList<Product> All();

            void Add(Product product);
        }

        """;

    private const string ProductCatalog = """
        using {{StoreNamespace}};
        using {{DomainNamespace}};

        namespace {{Namespace}};

        public sealed class ProductCatalog(IProductStore store)
        {
            public IReadOnlyList<Product> All() => store.All();

            public Product Add(string name)
            {
                var product = new Product(Guid.NewGuid(), name);
                store.Add(product);
                return product;
            }
        }

        """;

    private const string InMemoryProductStore = """
        using {{StoreNamespace}};
        using {{DomainNamespace}};

        namespace {{Namespace}};

        public sealed class InMemoryProductStore : IProductStore
        {
            private readonly List<Product> _products = [];

            public IReadOnlyList<Product> All() => _products;

            public void Add(Product product) => _products.Add(product);
        }

        """;

    private const string HostProgram = """
        using {{StoreNamespace}};
        using {{CatalogNamespace}};
        using {{AdapterNamespace}};

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<IProductStore, InMemoryProductStore>();
        builder.Services.AddSingleton<ProductCatalog>();

        var app = builder.Build();

        app.MapGet("/", () => Results.Ok(new { Status = "running" }));
        app.MapGet("/health", () => Results.Ok(new { Status = "healthy" }));
        app.MapGet("/products", (ProductCatalog catalog) => Results.Ok(catalog.All()));
        app.MapPost("/products", (CreateProduct request, ProductCatalog catalog) =>
        {
            var product = catalog.Add(request.Name);
            return Results.Created($"/products/{product.Id}", product);
        });

        app.Run();

        public sealed record CreateProduct(string Name);

        """;

    private const string AppSettingsTemplate = """
        {
          "Logging": {
            "LogLevel": {
              "Default": "Information",
              "Microsoft.AspNetCore": "Warning"
            }
          },
          "AllowedHosts": "*"
        }

        """;

    private const string AppSettingsDevelopmentTemplate = """
        {
          "Logging": {
            "LogLevel": {
              "Default": "Information",
              "Microsoft.AspNetCore": "Information"
            }
          }
        }

        """;

    private const string LaunchSettingsTemplate = """
        {
          "$schema": "https://json.schemastore.org/launchsettings.json",
          "profiles": {
            "http": {
              "commandName": "Project",
              "dotnetRunMessages": true,
              "launchBrowser": false,
              "applicationUrl": "http://localhost:5080",
              "environmentVariables": {
                "ASPNETCORE_ENVIRONMENT": "Development"
              }
            }
          }
        }

        """;

    private const string GitIgnoreTemplate = """
        bin/
        obj/
        .vs/
        *.user

        """;

    private const string ReadmeTemplate = """
        # {{ProjectName}}

        Project generated with [Laranet](https://github.com/TheJavaZ/Laranet).

        ## Getting started

        ```bash
        {{RunCommand}}
        ```
        {{Projects}}
        """;

    private const string ProjectsSectionTemplate = """

        ## Projects

        {{ProjectList}}
        """;

    /// <summary>
    /// Gets the template names accepted on the command line.
    /// </summary>
    public static IReadOnlyList<string> Names { get; } =
    [
        "minimal-api",
        "mvc",
        "layered",
        "hexagonal",
        "modular",
        "console",
    ];

    /// <summary>
    /// Parses a template name supplied on the command line.
    /// </summary>
    /// <param name="value">The template name, or <see langword="null"/> for the default.</param>
    /// <returns>The parsed template.</returns>
    /// <exception cref="CliException">The template name is not known.</exception>
    public static ProjectTemplate Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ProjectTemplate.MinimalApi;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "minimal-api" or "minimal" or "webapi" or "web-api" => ProjectTemplate.MinimalApi,
            "mvc" => ProjectTemplate.Mvc,
            "layered" => ProjectTemplate.Layered,
            "hexagonal" => ProjectTemplate.Hexagonal,
            "modular" => ProjectTemplate.Modular,
            "console" => ProjectTemplate.Console,
            _ => throw new CliException(
                $"'{value}' is not a known template. Use {string.Join(", ", Names)}."),
        };
    }

    /// <summary>
    /// Determines whether a template generates more than one project.
    /// </summary>
    /// <param name="template">The project template.</param>
    /// <returns><see langword="true"/> for layered and hexagonal templates.</returns>
    public static bool IsMultiProject(ProjectTemplate template)
    {
        return template is ProjectTemplate.Layered or ProjectTemplate.Hexagonal;
    }

    /// <summary>
    /// Gets the SDK used by a single-project template.
    /// </summary>
    /// <param name="template">The project template.</param>
    /// <returns>The SDK name.</returns>
    public static string Sdk(ProjectTemplate template)
    {
        return template switch
        {
            ProjectTemplate.MinimalApi
                or ProjectTemplate.Mvc
                or ProjectTemplate.Modular => WebSdk,
            ProjectTemplate.Console => LibrarySdk,
            _ => throw new ArgumentOutOfRangeException(nameof(template), template, null),
        };
    }

    /// <summary>
    /// Builds the projects that compose a multi-project template.
    /// </summary>
    /// <param name="template">The project template.</param>
    /// <param name="projectName">The project folder name.</param>
    /// <param name="rootNamespace">The root namespace of the application.</param>
    /// <returns>The projects, in dependency order.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The template is not multi-project.</exception>
    public static IReadOnlyList<ProjectLayer> Layers(
        ProjectTemplate template,
        string projectName,
        string rootNamespace)
    {
        if (!IsMultiProject(template))
        {
            throw new ArgumentOutOfRangeException(
                nameof(template),
                template,
                "The template does not generate multiple projects.");
        }

        var domain = $"{projectName}.Domain";
        var application = $"{projectName}.Application";
        var infrastructure = $"{projectName}.Infrastructure";
        var host = $"{projectName}.Web";

        return
        [
            new ProjectLayer(
                domain,
                $"{rootNamespace}.Domain",
                LibrarySdk,
                ProjectLayerRole.Domain,
                []),
            new ProjectLayer(
                application,
                $"{rootNamespace}.Application",
                LibrarySdk,
                ProjectLayerRole.Application,
                [domain]),
            new ProjectLayer(
                infrastructure,
                $"{rootNamespace}.Infrastructure",
                LibrarySdk,
                ProjectLayerRole.Infrastructure,
                [application, domain]),
            new ProjectLayer(
                host,
                $"{rootNamespace}.Web",
                WebSdk,
                ProjectLayerRole.Host,
                [application, infrastructure]),
        ];
    }

    /// <summary>
    /// Gets the host project path relative to the project root.
    /// </summary>
    /// <param name="template">The project template.</param>
    /// <param name="projectName">The project folder name.</param>
    /// <returns>The host project path, or <see langword="null"/> for single-project templates.</returns>
    public static string? HostProjectPath(ProjectTemplate template, string projectName)
    {
        return IsMultiProject(template)
            ? $"src/{projectName}.Web/{projectName}.Web.csproj"
            : null;
    }

    /// <summary>
    /// Generates a project file for a single-project template.
    /// </summary>
    /// <param name="template">The project template.</param>
    /// <param name="rootNamespace">The root namespace.</param>
    /// <param name="packages">The Laranet packages to reference.</param>
    /// <returns>The project file content.</returns>
    public static string ProjectFile(
        ProjectTemplate template,
        string rootNamespace,
        IReadOnlyList<string> packages)
    {
        return BuildProjectFile(
            Sdk(template),
            rootNamespace,
            isExecutable: template == ProjectTemplate.Console,
            projectReferences: [],
            packages);
    }

    /// <summary>
    /// Generates the project file for a layer of a multi-project template.
    /// </summary>
    /// <param name="layer">The project layer.</param>
    /// <param name="packages">The Laranet packages to reference.</param>
    /// <returns>The project file content.</returns>
    public static string LayerProjectFile(
        ProjectLayer layer,
        IReadOnlyList<string> packages)
    {
        return BuildProjectFile(
            layer.Sdk,
            layer.RootNamespace,
            isExecutable: false,
            layer.ProjectReferences,
            packages);
    }

    /// <summary>
    /// Generates the files owned by a single-project template, excluding settings files.
    /// </summary>
    /// <param name="template">The project template.</param>
    /// <param name="projectName">The project folder name.</param>
    /// <param name="rootNamespace">The root namespace.</param>
    /// <returns>The relative paths and contents.</returns>
    public static IReadOnlyList<(string Path, string Content)> SingleProjectContents(
        ProjectTemplate template,
        string projectName,
        string rootNamespace)
    {
        return template switch
        {
            ProjectTemplate.MinimalApi =>
            [
                ("Program.cs", MinimalApiProgram),
            ],
            ProjectTemplate.Console =>
            [
                ("Program.cs", Render(ConsoleProgram, ("{{Namespace}}", rootNamespace))),
            ],
            ProjectTemplate.Mvc =>
            [
                ("Program.cs", MvcProgram),
                (
                    "Controllers/HomeController.cs",
                    Render(MvcHomeController, ("{{Namespace}}", $"{rootNamespace}.Controllers"))),
                (
                    "Views/Home/Index.cshtml",
                    Render(MvcIndexView, ("{{ProjectName}}", projectName))),
                (
                    "Views/Shared/_Layout.cshtml",
                    Render(MvcLayoutView, ("{{ProjectName}}", projectName))),
                ("Views/_ViewImports.cshtml", MvcViewImports),
                ("Views/_ViewStart.cshtml", MvcViewStart),
            ],
            ProjectTemplate.Modular =>
            [
                (
                    "Program.cs",
                    Render(
                        ModularProgram,
                        ("{{FeaturesNamespace}}", $"{rootNamespace}.Features.Catalog"))),
                (
                    "Features/Catalog/Product.cs",
                    Render(
                        ModularProduct,
                        ("{{Namespace}}", $"{rootNamespace}.Features.Catalog"))),
                (
                    "Features/Catalog/CatalogService.cs",
                    Render(
                        ModularCatalogService,
                        ("{{Namespace}}", $"{rootNamespace}.Features.Catalog"))),
                (
                    "Features/Catalog/CatalogEndpoints.cs",
                    Render(
                        ModularCatalogEndpoints,
                        ("{{Namespace}}", $"{rootNamespace}.Features.Catalog"))),
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(template), template, null),
        };
    }

    /// <summary>
    /// Generates the files owned by one project of a multi-project template.
    /// </summary>
    /// <param name="template">The project template.</param>
    /// <param name="rootNamespace">The root namespace of the application.</param>
    /// <param name="layer">The project layer.</param>
    /// <returns>The relative paths and contents.</returns>
    public static IReadOnlyList<(string Path, string Content)> LayerContents(
        ProjectTemplate template,
        string rootNamespace,
        ProjectLayer layer)
    {
        if (!IsMultiProject(template))
        {
            throw new ArgumentOutOfRangeException(
                nameof(template),
                template,
                "The template does not generate multiple projects.");
        }

        return layer.Role switch
        {
            ProjectLayerRole.Domain =>
            [
                (
                    "Entities/Product.cs",
                    Render(ProductEntity, ("{{Namespace}}", $"{layer.RootNamespace}.Entities"))),
            ],
            ProjectLayerRole.Application =>
                ApplicationContents(template, rootNamespace, layer),
            ProjectLayerRole.Infrastructure =>
                InfrastructureContents(template, rootNamespace, layer),
            ProjectLayerRole.Host =>
            [
                ("Program.cs", HostProgramContent(template, rootNamespace)),
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, null),
        };
    }

    /// <summary>
    /// Gets the application settings file content.
    /// </summary>
    /// <returns>The file content.</returns>
    public static string AppSettings()
    {
        return AppSettingsTemplate;
    }

    /// <summary>
    /// Gets the development application settings file content.
    /// </summary>
    /// <returns>The file content.</returns>
    public static string AppSettingsDevelopment()
    {
        return AppSettingsDevelopmentTemplate;
    }

    /// <summary>
    /// Gets the launch settings file content.
    /// </summary>
    /// <returns>The file content.</returns>
    public static string LaunchSettings()
    {
        return LaunchSettingsTemplate;
    }

    /// <summary>
    /// Gets the content of the generated .gitignore file.
    /// </summary>
    /// <returns>The file content.</returns>
    public static string GitIgnore()
    {
        return GitIgnoreTemplate;
    }

    /// <summary>
    /// Generates the content of the project README.
    /// </summary>
    /// <param name="projectName">The project folder name.</param>
    /// <param name="runCommand">The command that runs the host project.</param>
    /// <param name="layers">The generated projects, when the template has more than one.</param>
    /// <returns>The file content.</returns>
    public static string Readme(
        string projectName,
        string runCommand,
        IReadOnlyList<ProjectLayer>? layers = null)
    {
        var projects = layers is null
            ? string.Empty
            : ProjectsSectionTemplate.Replace(
                "{{ProjectList}}",
                string.Join(
                    '\n',
                    layers.Select(layer =>
                        $"- `src/{layer.FolderName}/{layer.FolderName}.csproj`")),
                StringComparison.Ordinal);

        return ReadmeTemplate
            .Replace("{{ProjectName}}", projectName, StringComparison.Ordinal)
            .Replace("{{RunCommand}}", runCommand, StringComparison.Ordinal)
            .Replace("{{Projects}}", projects, StringComparison.Ordinal);
    }

    /// <summary>
    /// Generates a solution file.
    /// </summary>
    /// <param name="projectPaths">The project paths relative to the solution.</param>
    /// <returns>The file content.</returns>
    public static string Solution(IEnumerable<string> projectPaths)
    {
        var lines = new List<string> { "<Solution>" };

        foreach (var projectPath in projectPaths)
        {
            lines.Add($"  <Project Path=\"{projectPath}\" />");
        }

        lines.Add("</Solution>");

        return string.Join('\n', lines) + "\n";
    }

    private static IReadOnlyList<(string Path, string Content)> ApplicationContents(
        ProjectTemplate template,
        string rootNamespace,
        ProjectLayer layer)
    {
        var isHexagonal = template == ProjectTemplate.Hexagonal;
        var storeFolder = isHexagonal ? "Ports" : "Abstractions";
        var storeNamespace = $"{layer.RootNamespace}.{storeFolder}";
        var domainNamespace = $"{rootNamespace}.Domain.Entities";
        var catalogNamespace = $"{layer.RootNamespace}.Products";

        return
        [
            (
                $"{storeFolder}/IProductStore.cs",
                Render(
                    ProductStoreInterface,
                    ("{{Namespace}}", storeNamespace),
                    ("{{DomainNamespace}}", domainNamespace))),
            (
                "Products/ProductCatalog.cs",
                Render(
                    ProductCatalog,
                    ("{{Namespace}}", catalogNamespace),
                    ("{{StoreNamespace}}", storeNamespace),
                    ("{{DomainNamespace}}", domainNamespace))),
        ];
    }

    private static IReadOnlyList<(string Path, string Content)> InfrastructureContents(
        ProjectTemplate template,
        string rootNamespace,
        ProjectLayer layer)
    {
        var isHexagonal = template == ProjectTemplate.Hexagonal;
        var storeFolder = isHexagonal ? "Ports" : "Abstractions";
        var adapterFolder = isHexagonal ? "Adapters" : "Persistence";
        var storeNamespace = $"{rootNamespace}.Application.{storeFolder}";
        var domainNamespace = $"{rootNamespace}.Domain.Entities";
        var adapterNamespace = $"{layer.RootNamespace}.{adapterFolder}";

        return
        [
            (
                $"{adapterFolder}/InMemoryProductStore.cs",
                Render(
                    InMemoryProductStore,
                    ("{{Namespace}}", adapterNamespace),
                    ("{{StoreNamespace}}", storeNamespace),
                    ("{{DomainNamespace}}", domainNamespace))),
        ];
    }

    private static string HostProgramContent(
        ProjectTemplate template,
        string rootNamespace)
    {
        var isHexagonal = template == ProjectTemplate.Hexagonal;
        var storeFolder = isHexagonal ? "Ports" : "Abstractions";
        var adapterFolder = isHexagonal ? "Adapters" : "Persistence";
        var applicationNamespace = $"{rootNamespace}.Application";

        return Render(
            HostProgram,
            ("{{StoreNamespace}}", $"{applicationNamespace}.{storeFolder}"),
            ("{{CatalogNamespace}}", $"{applicationNamespace}.Products"),
            ("{{AdapterNamespace}}", $"{rootNamespace}.Infrastructure.{adapterFolder}"));
    }

    private static string BuildProjectFile(
        string sdk,
        string rootNamespace,
        bool isExecutable,
        IReadOnlyList<string> projectReferences,
        IReadOnlyList<string> packages)
    {
        var lines = new List<string>
        {
            $"<Project Sdk=\"{sdk}\">",
            "",
            "  <PropertyGroup>",
            "    <TargetFramework>net10.0</TargetFramework>",
        };

        if (isExecutable)
        {
            lines.Add("    <OutputType>Exe</OutputType>");
        }

        lines.Add("    <Nullable>enable</Nullable>");
        lines.Add("    <ImplicitUsings>enable</ImplicitUsings>");
        lines.Add($"    <RootNamespace>{rootNamespace}</RootNamespace>");
        lines.Add("  </PropertyGroup>");

        if (projectReferences.Count > 0)
        {
            lines.Add("");
            lines.Add("  <ItemGroup>");

            foreach (var reference in projectReferences)
            {
                lines.Add(
                    $"    <ProjectReference Include=\"../{reference}/{reference}.csproj\" />");
            }

            lines.Add("  </ItemGroup>");
        }

        if (packages.Count > 0)
        {
            lines.Add("");
            lines.Add("  <ItemGroup>");

            foreach (var package in packages)
            {
                lines.Add(
                    $"    <PackageReference Include=\"{package}\" Version=\"{LaranetVersion.Current}\" />");
            }

            lines.Add("  </ItemGroup>");
        }

        lines.Add("");
        lines.Add("</Project>");

        return string.Join('\n', lines) + "\n";
    }

    private static string Render(
        string template,
        params (string Token, string Value)[] replacements)
    {
        var content = template;

        foreach (var (token, value) in replacements)
        {
            content = content.Replace(token, value, StringComparison.Ordinal);
        }

        return content;
    }
}
