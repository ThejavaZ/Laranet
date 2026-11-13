using Laranet.Cli;
using Laranet.Tests.TestSupport;

namespace Laranet.Tests.Integration.Cli;

public class ProjectTemplateTests
{
    [Theory]
    [InlineData("minimal-api")]
    [InlineData("minimal")]
    [InlineData("webapi")]
    [InlineData("web-api")]
    public void New_ShouldCreateMinimalApiProject_WhenTemplateIsUsed(string templateName)
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-api",
            "--template",
            templateName,
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);

        var projectDirectory = directory.GetPath("my-api");
        var project = File.ReadAllText(
            Path.Combine(projectDirectory, "my-api.csproj"));

        Assert.Contains("<Project Sdk=\"Microsoft.NET.Sdk.Web\">", project);
        Assert.Contains(
            "WebApplication.CreateBuilder",
            File.ReadAllText(Path.Combine(projectDirectory, "Program.cs")));
    }

    [Fact]
    public void New_ShouldCreateMvcProject()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-web",
            "--template",
            "mvc",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);

        var projectDirectory = directory.GetPath("my-web");

        Assert.True(File.Exists(
            Path.Combine(projectDirectory, "Controllers", "HomeController.cs")));
        Assert.True(File.Exists(
            Path.Combine(projectDirectory, "Views", "Home", "Index.cshtml")));
        Assert.True(File.Exists(
            Path.Combine(projectDirectory, "Views", "Shared", "_Layout.cshtml")));

        var program = File.ReadAllText(Path.Combine(projectDirectory, "Program.cs"));

        Assert.Contains("AddControllersWithViews", program);
        Assert.Contains("MapControllerRoute", program);
        Assert.Contains(
            "namespace MyWeb.Controllers;",
            File.ReadAllText(
                Path.Combine(projectDirectory, "Controllers", "HomeController.cs")));
    }

    [Fact]
    public void New_ShouldCreateModularProjectWithFeatureFolders()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-api",
            "--template",
            "modular",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);

        var projectDirectory = directory.GetPath("my-api");
        var featureDirectory = Path.Combine(projectDirectory, "Features", "Catalog");

        Assert.True(File.Exists(
            Path.Combine(featureDirectory, "CatalogEndpoints.cs")));
        Assert.True(File.Exists(
            Path.Combine(featureDirectory, "CatalogService.cs")));
        Assert.True(File.Exists(
            Path.Combine(featureDirectory, "Product.cs")));
        Assert.False(Directory.Exists(
            Path.Combine(projectDirectory, "Controllers")));

        Assert.Contains(
            "MapCatalogEndpoints",
            File.ReadAllText(Path.Combine(projectDirectory, "Program.cs")));
        Assert.Contains(
            "namespace MyApi.Features.Catalog;",
            File.ReadAllText(Path.Combine(featureDirectory, "CatalogService.cs")));
    }

    [Fact]
    public void New_ShouldCreateLayeredProjectsWithReferences()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-api",
            "--template",
            "layered",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);

        var projectDirectory = directory.GetPath("my-api");
        var solution = Path.Combine(projectDirectory, "my-api.slnx");

        Assert.True(File.Exists(solution));
        Assert.True(File.Exists(
            Path.Combine(projectDirectory, "src", "my-api.Domain", "my-api.Domain.csproj")));
        Assert.True(File.Exists(
            Path.Combine(projectDirectory, "src", "my-api.Application", "my-api.Application.csproj")));
        Assert.True(File.Exists(
            Path.Combine(projectDirectory, "src", "my-api.Infrastructure", "my-api.Infrastructure.csproj")));
        Assert.True(File.Exists(
            Path.Combine(projectDirectory, "src", "my-api.Web", "my-api.Web.csproj")));

        var application = File.ReadAllText(
            Path.Combine(projectDirectory, "src", "my-api.Application", "my-api.Application.csproj"));
        var infrastructure = File.ReadAllText(
            Path.Combine(projectDirectory, "src", "my-api.Infrastructure", "my-api.Infrastructure.csproj"));
        var web = File.ReadAllText(
            Path.Combine(projectDirectory, "src", "my-api.Web", "my-api.Web.csproj"));

        Assert.Contains(
            "<ProjectReference Include=\"../my-api.Domain/my-api.Domain.csproj\" />",
            application);
        Assert.Contains(
            "<ProjectReference Include=\"../my-api.Application/my-api.Application.csproj\" />",
            infrastructure);
        Assert.Contains(
            "<ProjectReference Include=\"../my-api.Infrastructure/my-api.Infrastructure.csproj\" />",
            web);

        var solutionContent = File.ReadAllText(solution);
        Assert.Contains("<Project Path=\"src/my-api.Domain/my-api.Domain.csproj\" />", solutionContent);
        Assert.Contains("<Project Path=\"src/my-api.Web/my-api.Web.csproj\" />", solutionContent);

        Assert.True(File.Exists(
            Path.Combine(
                projectDirectory,
                "src",
                "my-api.Application",
                "Abstractions",
                "IProductStore.cs")));
        Assert.True(File.Exists(
            Path.Combine(
                projectDirectory,
                "src",
                "my-api.Infrastructure",
                "Persistence",
                "InMemoryProductStore.cs")));

        Assert.Contains(
            "dotnet run --project src/my-api.Web/my-api.Web.csproj",
            File.ReadAllText(Path.Combine(projectDirectory, "README.md")));
        Assert.Contains(
            "dotnet run --project src/my-api.Web/my-api.Web.csproj",
            result.Output);
    }

    [Fact]
    public void New_ShouldCreateHexagonalProjectsWithPortsAndAdapters()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-api",
            "--template",
            "hexagonal",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);

        var projectDirectory = directory.GetPath("my-api");
        var ports = Path.Combine(
            projectDirectory,
            "src",
            "my-api.Application",
            "Ports",
            "IProductStore.cs");
        var adapters = Path.Combine(
            projectDirectory,
            "src",
            "my-api.Infrastructure",
            "Adapters",
            "InMemoryProductStore.cs");

        Assert.True(File.Exists(ports));
        Assert.True(File.Exists(adapters));
        Assert.False(Directory.Exists(
            Path.Combine(
                projectDirectory,
                "src",
                "my-api.Application",
                "Abstractions")));

        Assert.Contains(
            "namespace MyApi.Application.Ports;",
            File.ReadAllText(ports));
        Assert.Contains(
            "namespace MyApi.Infrastructure.Adapters;",
            File.ReadAllText(adapters));
    }

    [Fact]
    public void New_ShouldAddPackagesToHostProject_WhenTemplateIsMultiProject()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-api",
            "--template",
            "layered",
            "--with-data",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);

        var projectDirectory = directory.GetPath("my-api");
        var web = File.ReadAllText(
            Path.Combine(projectDirectory, "src", "my-api.Web", "my-api.Web.csproj"));
        var domain = File.ReadAllText(
            Path.Combine(projectDirectory, "src", "my-api.Domain", "my-api.Domain.csproj"));

        Assert.Contains("Include=\"Laranet.Data\"", web);
        Assert.DoesNotContain("PackageReference", domain);
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
