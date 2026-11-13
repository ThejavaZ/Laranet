using Laranet.Cli;
using Laranet.Tests.TestSupport;

namespace Laranet.Tests.Integration.Cli;

public class CrudGenerationTests
{
    [Fact]
    public void MakeCrud_ShouldGenerateSimpleProfile_ByDefault()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:crud",
            "product",
            "--context",
            "Test.App.AppDbContext",
            "--namespace",
            "Test.App",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(directory.GetPath("Models", "Product.cs")));
        Assert.True(File.Exists(directory.GetPath("Contracts", "ProductRequest.cs")));
        Assert.True(File.Exists(directory.GetPath("Contracts", "ProductResponse.cs")));
        Assert.True(File.Exists(directory.GetPath("Controllers", "ProductController.cs")));

        Assert.False(File.Exists(directory.GetPath("Services", "ProductService.cs")));
        Assert.False(File.Exists(directory.GetPath(
            "Extensions",
            "ProductServiceCollectionExtensions.cs")));

        var controller = File.ReadAllText(
            directory.GetPath("Controllers", "ProductController.cs"));

        Assert.Contains("public sealed class ProductController(global::Test.App.AppDbContext db)", controller);
        Assert.Contains("db.Set<Product>()", controller);
        Assert.Contains("[Route(\"api/products\")]", controller);
    }

    [Fact]
    public void MakeCrud_ShouldGenerateStandardProfile()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:crud",
            "product",
            "--profile",
            "standard",
            "--fields",
            "Name:string,Price:decimal",
            "--context",
            "Test.App.AppDbContext",
            "--namespace",
            "Test.App",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(directory.GetPath("Services", "ProductService.cs")));
        Assert.True(File.Exists(directory.GetPath(
            "Extensions",
            "ProductServiceCollectionExtensions.cs")));

        var controller = File.ReadAllText(
            directory.GetPath("Controllers", "ProductController.cs"));
        Assert.Contains("ProductController(ProductService service)", controller);
        Assert.Contains("await service.GetAllAsync(cancellationToken)", controller);

        var registration = File.ReadAllText(directory.GetPath(
            "Extensions",
            "ProductServiceCollectionExtensions.cs"));
        Assert.Contains("services.AddScoped<ProductService>();", registration);
        Assert.DoesNotContain("using Test.App.Contracts;", registration);
    }

    [Fact]
    public void MakeCrud_ShouldGenerateCleanProfileLayers()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:crud",
            "product",
            "--profile",
            "clean",
            "--context",
            "Test.App.AppDbContext",
            "--namespace",
            "Test.App",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(directory.GetPath("Domain", "Entities", "Product.cs")));
        Assert.True(File.Exists(directory.GetPath("Application", "Products", "ProductService.cs")));
        Assert.True(File.Exists(directory.GetPath("Application", "Products", "ProductRequest.cs")));
        Assert.True(File.Exists(directory.GetPath("Application", "Products", "ProductResponse.cs")));
        Assert.True(File.Exists(directory.GetPath("Application", "Abstractions", "IProductRepository.cs")));
        Assert.True(File.Exists(directory.GetPath("Infrastructure", "Persistence", "ProductRepository.cs")));
        Assert.True(File.Exists(directory.GetPath("Controllers", "ProductController.cs")));

        var registration = File.ReadAllText(directory.GetPath(
            "Extensions",
            "ProductServiceCollectionExtensions.cs"));
        Assert.Contains(
            "services.AddScoped<IProductRepository, ProductRepository>();",
            registration);
        Assert.Contains("services.AddScoped<ProductService>();", registration);

        var service = File.ReadAllText(
            directory.GetPath("Application", "Products", "ProductService.cs"));
        Assert.Contains("ProductService(IProductRepository repository)", service);
    }

    [Fact]
    public void MakeCrud_ShouldGenerateModularProfileInFeatureFolder()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:crud",
            "product",
            "--profile",
            "modular",
            "--context",
            "Test.App.AppDbContext",
            "--namespace",
            "Test.App",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);

        var feature = directory.GetPath("Features", "Products");

        Assert.True(File.Exists(Path.Combine(feature, "Product.cs")));
        Assert.True(File.Exists(Path.Combine(feature, "ProductRequest.cs")));
        Assert.True(File.Exists(Path.Combine(feature, "ProductResponse.cs")));
        Assert.True(File.Exists(Path.Combine(feature, "ProductService.cs")));
        Assert.True(File.Exists(Path.Combine(feature, "ProductController.cs")));
        Assert.True(File.Exists(Path.Combine(
            feature,
            "ProductServiceCollectionExtensions.cs")));

        var entity = File.ReadAllText(Path.Combine(feature, "Product.cs"));
        Assert.Contains("namespace Test.App.Features.Products;", entity);
    }

    [Fact]
    public void MakeCrud_ShouldUseFieldsInEveryGeneratedFile()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:crud",
            "product",
            "--fields",
            "Name:string,Price:decimal,Description:string?",
            "--context",
            "Test.App.AppDbContext",
            "--namespace",
            "Test.App",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);

        var entity = File.ReadAllText(directory.GetPath("Models", "Product.cs"));
        Assert.Contains("public string Name { get; set; } = string.Empty;", entity);
        Assert.Contains("public decimal Price { get; set; }", entity);
        Assert.Contains("public string? Description { get; set; }", entity);

        var request = File.ReadAllText(
            directory.GetPath("Contracts", "ProductRequest.cs"));
        Assert.Contains("[Required]", request);
        Assert.Contains("[StringLength(200)]", request);
        Assert.Contains("using System.ComponentModel.DataAnnotations;", request);

        var response = File.ReadAllText(
            directory.GetPath("Contracts", "ProductResponse.cs"));
        Assert.Contains(
            "ProductResponse(int Id, string Name, decimal Price, string? Description)",
            response);

        var controller = File.ReadAllText(
            directory.GetPath("Controllers", "ProductController.cs"));
        Assert.Contains("Name = request.Name,", controller);
        Assert.Contains("product.Description = request.Description;", controller);
    }

    [Fact]
    public void MakeCrud_ShouldEscapeEntityNamesThatBecomeReservedVariables()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:crud",
            "event",
            "--context",
            "Test.App.AppDbContext",
            "--namespace",
            "Test.App",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);

        var controller = File.ReadAllText(
            directory.GetPath("Controllers", "EventController.cs"));

        Assert.Contains("[Route(\"api/events\")]", controller);
        Assert.Contains("var @event = await db.Set<Event>()", controller);
        Assert.Contains("@event => new EventResponse(@event.Id", controller);
    }

    [Fact]
    public void MakeCrud_ShouldDiscoverTheOnlyDbContext()
    {
        using var directory = new TemporaryDirectory();
        WriteProject(directory, "Inferred.App");
        WriteContext(directory, "Inferred.App.Data", "InferredDbContext");

        var result = Run(
            "make:crud",
            "product",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);

        var controller = File.ReadAllText(
            directory.GetPath("Controllers", "ProductController.cs"));

        Assert.Contains(
            "public sealed class ProductController(global::Inferred.App.Data.InferredDbContext db)",
            controller);
    }

    [Fact]
    public void MakeCrud_ShouldReturnError_WhenMultipleContextsAreDiscovered()
    {
        using var directory = new TemporaryDirectory();
        WriteProject(directory, "Test.App");
        WriteContext(directory, "Test.App.Data", "FirstDbContext");
        WriteContext(directory, "Test.App.Data", "SecondDbContext");

        var result = Run(
            "make:crud",
            "product",
            "--namespace",
            "Test.App",
            "--output",
            directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Multiple DbContext types were found", result.Error);
        Assert.False(Directory.Exists(directory.GetPath("Models")));
    }

    [Fact]
    public void MakeCrud_ShouldReturnError_WhenNoContextCanBeFound()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:crud",
            "product",
            "--namespace",
            "Test.App",
            "--output",
            directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Could not find a .csproj file", result.Error);
        Assert.False(Directory.Exists(directory.GetPath("Models")));
    }

    [Fact]
    public void MakeCrud_ShouldReturnError_WhenContextIsInvalid()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:crud",
            "product",
            "--context",
            "9Invalid",
            "--namespace",
            "Test.App",
            "--output",
            directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("not a valid DbContext type", result.Error);
    }

    [Fact]
    public void MakeCrud_ShouldReturnError_WhenProfileIsUnknown()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:crud",
            "product",
            "--profile",
            "microservices",
            "--context",
            "Test.App.AppDbContext",
            "--namespace",
            "Test.App",
            "--output",
            directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("not a known profile", result.Error);
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("Name:money")]
    [InlineData("Id:int")]
    public void MakeCrud_ShouldReturnError_WhenFieldsAreInvalid(string fields)
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:crud",
            "product",
            "--fields",
            fields,
            "--context",
            "Test.App.AppDbContext",
            "--namespace",
            "Test.App",
            "--output",
            directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("error:", result.Error);
        Assert.False(Directory.Exists(directory.GetPath("Models")));
    }

    [Fact]
    public void MakeCrud_ShouldNotWriteAnyFile_WhenAFileConflicts()
    {
        using var directory = new TemporaryDirectory();
        var entityPath = directory.GetPath("Models", "Product.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(entityPath)!);
        File.WriteAllText(entityPath, "custom content");

        var result = Run(
            "make:crud",
            "product",
            "--context",
            "Test.App.AppDbContext",
            "--namespace",
            "Test.App",
            "--output",
            directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("already exists", result.Error);
        Assert.Equal("custom content", File.ReadAllText(entityPath));
        Assert.False(File.Exists(directory.GetPath("Contracts", "ProductRequest.cs")));
        Assert.False(File.Exists(directory.GetPath("Controllers", "ProductController.cs")));
    }

    [Fact]
    public void MakeCrud_ShouldOverwrite_WhenForceIsRequested()
    {
        using var directory = new TemporaryDirectory();
        var entityPath = directory.GetPath("Models", "Product.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(entityPath)!);
        File.WriteAllText(entityPath, "custom content");

        var result = Run(
            "make:crud",
            "product",
            "--context",
            "Test.App.AppDbContext",
            "--namespace",
            "Test.App",
            "--output",
            directory.Root,
            "--force");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("public sealed class Product", File.ReadAllText(entityPath));
    }

    [Fact]
    public void MakeCrud_ShouldNotWriteFiles_WhenDryRunIsRequested()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:crud",
            "product",
            "--context",
            "Test.App.AppDbContext",
            "--namespace",
            "Test.App",
            "--output",
            directory.Root,
            "--dry-run");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("would create", result.Output);
        Assert.False(Directory.Exists(directory.GetPath("Models")));
        Assert.False(Directory.Exists(directory.GetPath("Controllers")));
    }

    [Fact]
    public void MakeCrud_ShouldPlaceFilesFlat_WhenRequested()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:crud",
            "product",
            "--profile",
            "clean",
            "--context",
            "Test.App.AppDbContext",
            "--namespace",
            "Test.App",
            "--output",
            directory.Root,
            "--flat");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(directory.GetPath("Product.cs")));
        Assert.True(File.Exists(directory.GetPath("IProductRepository.cs")));
        Assert.True(File.Exists(directory.GetPath("ProductController.cs")));

        var entity = File.ReadAllText(directory.GetPath("Product.cs"));
        Assert.Contains("namespace Test.App;", entity);
        Assert.DoesNotContain("Test.App.Models", entity);
    }

    private static void WriteProject(TemporaryDirectory directory, string rootNamespace)
    {
        File.WriteAllText(
            directory.GetPath("Test.csproj"),
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <RootNamespace>{rootNamespace}</RootNamespace>
              </PropertyGroup>
            </Project>
            """);
    }

    private static void WriteContext(
        TemporaryDirectory directory,
        string @namespace,
        string typeName)
    {
        Directory.CreateDirectory(directory.GetPath("Data"));

        File.WriteAllText(
            directory.GetPath("Data", $"{typeName}.cs"),
            $$"""
            using Microsoft.EntityFrameworkCore;

            namespace {{@namespace}};

            public sealed class {{typeName}} : DbContext
            {
            }
            """);
    }

    private static CommandResult Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = CliApplication.Execute(args, output, error);

        return new CommandResult(exitCode, output.ToString(), error.ToString());
    }

    private sealed record CommandResult(int ExitCode, string Output, string Error);
}
