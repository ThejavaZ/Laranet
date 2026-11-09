using Laranet.Cli;
using Laranet.Tests.TestSupport;

namespace Laranet.Tests.Integration.Cli;

public class CliGenerationTests
{
    [Fact]
    public void MakeModel_ShouldGenerateModelFile()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:model",
            "product",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.Equal(0, result.ExitCode);

        var path = directory.GetPath("Models", "Product.cs");
        Assert.True(File.Exists(path));

        var content = File.ReadAllText(path);
        Assert.Contains("namespace Test.App.Models;", content);
        Assert.Contains("public class Product", content);
    }

    [Fact]
    public void MakeService_ShouldGenerateServiceFile()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:service",
            "product",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.Equal(0, result.ExitCode);

        var path = directory.GetPath("Services", "ProductService.cs");
        Assert.True(File.Exists(path));

        var content = File.ReadAllText(path);
        Assert.Contains("namespace Test.App.Services;", content);
        Assert.Contains("public sealed class ProductService", content);
    }

    [Fact]
    public void MakeService_ShouldNotDuplicateSuffix()
    {
        using var directory = new TemporaryDirectory();

        Run(
            "make:service",
            "ProductService",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.True(File.Exists(directory.GetPath("Services", "ProductService.cs")));
        Assert.False(File.Exists(directory.GetPath("Services", "ProductServiceService.cs")));
    }

    [Fact]
    public void MakeController_ShouldGenerateControllerFile()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:controller",
            "product",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.Equal(0, result.ExitCode);

        var path = directory.GetPath("Controllers", "ProductController.cs");
        Assert.True(File.Exists(path));

        var content = File.ReadAllText(path);
        Assert.Contains("using Microsoft.AspNetCore.Mvc;", content);
        Assert.Contains("namespace Test.App.Controllers;", content);
        Assert.Contains("[ApiController]", content);
        Assert.Contains("public sealed class ProductController : ControllerBase", content);
    }

    [Fact]
    public void MakeResource_ShouldGenerateRelatedFilesTogether()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:resource",
            "product-category",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(directory.GetPath("Models", "ProductCategory.cs")));
        Assert.True(File.Exists(directory.GetPath("Services", "ProductCategoryService.cs")));
        Assert.True(File.Exists(directory.GetPath("Controllers", "ProductCategoryController.cs")));
    }

    [Fact]
    public void MakeCommand_ShouldInferNamespaceFromProject()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(
            directory.GetPath("Test.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <RootNamespace>Inferred.App</RootNamespace>
              </PropertyGroup>
            </Project>
            """);

        var result = Run(
            "make:model",
            "product",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(
            "namespace Inferred.App.Models;",
            File.ReadAllText(directory.GetPath("Models", "Product.cs")));
    }

    [Fact]
    public void MakeCommand_ShouldReturnError_WhenNamespaceCannotBeInferred()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:model",
            "product",
            "--output",
            directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Could not infer a namespace", result.Error);
        Assert.False(Directory.Exists(directory.GetPath("Models")));
    }

    [Fact]
    public void MakeCommand_ShouldPlaceFilesFlat_WhenRequested()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:model",
            "product",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App",
            "--flat");

        Assert.Equal(0, result.ExitCode);

        var path = directory.GetPath("Product.cs");
        Assert.True(File.Exists(path));

        var content = File.ReadAllText(path);
        Assert.Contains("namespace Test.App;", content);
        Assert.DoesNotContain("Test.App.Models", content);
    }

    [Fact]
    public void MakeCommand_ShouldRefuseToOverwriteExistingFiles()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.GetPath("Models", "Product.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "custom content");

        var result = Run(
            "make:model",
            "product",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("already exists", result.Error);
        Assert.Equal("custom content", File.ReadAllText(path));
    }

    [Fact]
    public void MakeResource_ShouldNotWriteAnyFile_WhenAnyFileConflicts()
    {
        using var directory = new TemporaryDirectory();
        var modelPath = directory.GetPath("Models", "Product.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
        File.WriteAllText(modelPath, "custom content");

        var result = Run(
            "make:resource",
            "product",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.Equal(1, result.ExitCode);
        Assert.False(File.Exists(directory.GetPath("Services", "ProductService.cs")));
        Assert.False(File.Exists(directory.GetPath("Controllers", "ProductController.cs")));
    }

    [Fact]
    public void MakeCommand_ShouldOverwrite_WhenForceIsRequested()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.GetPath("Models", "Product.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "custom content");

        var result = Run(
            "make:model",
            "product",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App",
            "--force");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("namespace Test.App.Models;", File.ReadAllText(path));
    }

    [Fact]
    public void MakeCommand_ShouldNotWriteFiles_WhenDryRunIsRequested()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:model",
            "product",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App",
            "--dry-run");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("would create", result.Output);
        Assert.False(Directory.Exists(directory.GetPath("Models")));
    }

    [Fact]
    public void MakeCommand_ShouldCreateOutputDirectory()
    {
        using var directory = new TemporaryDirectory();
        var output = directory.GetPath("src", "Feature");

        var result = Run(
            "make:model",
            "product",
            "--output",
            output,
            "--namespace",
            "Test.App");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(output, "Models", "Product.cs")));
    }

    [Fact]
    public void MakeCommand_ShouldReturnError_WhenNameIsInvalid()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:model",
            "123product",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("not a valid name", result.Error);
    }

    [Fact]
    public void MakeCommand_ShouldReturnError_WhenNameIsMissing()
    {
        using var directory = new TemporaryDirectory();

        var result = Run("make:model", "--output", directory.Root);

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public void MakeMiddleware_ShouldGenerateMiddlewareFile()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:middleware",
            "request-logging",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.Equal(0, result.ExitCode);

        var path = directory.GetPath("Middleware", "RequestLoggingMiddleware.cs");
        Assert.True(File.Exists(path));

        var content = File.ReadAllText(path);
        Assert.Contains("using Microsoft.AspNetCore.Http;", content);
        Assert.Contains("namespace Test.App.Middleware;", content);
        Assert.Contains("public sealed class RequestLoggingMiddleware", content);
        Assert.Contains("RequestDelegate _next", content);
        Assert.Contains("public async Task InvokeAsync(HttpContext context)", content);
        Assert.Contains("await _next(context);", content);
    }

    [Fact]
    public void MakeMiddleware_ShouldNotDuplicateSuffix()
    {
        using var directory = new TemporaryDirectory();

        Run(
            "make:middleware",
            "AuthMiddleware",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.True(File.Exists(directory.GetPath("Middleware", "AuthMiddleware.cs")));
        Assert.False(File.Exists(directory.GetPath("Middleware", "AuthMiddlewareMiddleware.cs")));
    }

    [Fact]
    public void MakeRepository_ShouldGenerateRepositoryFile()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "make:repository",
            "product",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.Equal(0, result.ExitCode);

        var path = directory.GetPath("Repositories", "ProductRepository.cs");
        Assert.True(File.Exists(path));

        var content = File.ReadAllText(path);
        Assert.Contains("namespace Test.App.Repositories;", content);
        Assert.Contains("public sealed class ProductRepository", content);
        Assert.Contains("Use it only when the abstraction is justified.", content);
    }

    [Fact]
    public void MakeRepository_ShouldNotDuplicateSuffix()
    {
        using var directory = new TemporaryDirectory();

        Run(
            "make:repository",
            "ProductRepository",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.True(File.Exists(directory.GetPath("Repositories", "ProductRepository.cs")));
        Assert.False(File.Exists(directory.GetPath("Repositories", "ProductRepositoryRepository.cs")));
    }

    [Fact]
    public void MakeResource_ShouldNotGenerateOptionalFiles()
    {
        using var directory = new TemporaryDirectory();

        Run(
            "make:resource",
            "product",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.False(File.Exists(directory.GetPath("Middleware", "ProductMiddleware.cs")));
        Assert.False(File.Exists(directory.GetPath("Repositories", "ProductRepository.cs")));
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
