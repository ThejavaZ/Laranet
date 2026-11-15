using System.Text.Json;
using Laranet.Cli;
using Laranet.Tests.TestSupport;

namespace Laranet.Tests.Integration.Cli;

public class DoctorTests
{
    [Fact]
    public void Doctor_ShouldReportCleanProject()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(
            directory.GetPath("Clean.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var result = Run("doctor", "--path", directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("No diagnostics found", result.Output);
    }

    [Fact]
    public void Doctor_ShouldReportCircularProjectReferences()
    {
        using var directory = new TemporaryDirectory();
        var first = directory.GetPath("First");
        var second = directory.GetPath("Second");
        Directory.CreateDirectory(first);
        Directory.CreateDirectory(second);
        File.WriteAllText(
            directory.GetPath("First", "First.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
              <ItemGroup><ProjectReference Include="../Second/Second.csproj" /></ItemGroup>
            </Project>
            """);
        File.WriteAllText(
            directory.GetPath("Second", "Second.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
              <ItemGroup><ProjectReference Include="../First/First.csproj" /></ItemGroup>
            </Project>
            """);

        var result = Run("doctor", "--path", directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("LARANET002", result.Output);
        Assert.Contains("Circular project reference", result.Output);
    }

    [Fact]
    public void Doctor_ShouldReportMissingProjectReferences()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(
            directory.GetPath("Broken.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
              <ItemGroup><ProjectReference Include="Missing/Missing.csproj" /></ItemGroup>
            </Project>
            """);

        var result = Run("doctor", "--path", directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("LARANET001", result.Output);
        Assert.Contains("does not exist", result.Output);
    }

    [Fact]
    public void Doctor_ShouldReportUnsupportedVersions()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(
            directory.GetPath("Legacy.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net9.0</TargetFramework></PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Laranet.Data" Version="1.0.0" />
                <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
              </ItemGroup>
            </Project>
            """);

        var result = Run("doctor", "--path", directory.Root, "--json");
        using var document = JsonDocument.Parse(result.Output);
        var codes = document.RootElement
            .GetProperty("diagnostics")
            .EnumerateArray()
            .Select(diagnostic => diagnostic.GetProperty("code").GetString())
            .ToArray();

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("LARANET003", codes);
        Assert.Contains("LARANET004", codes);
        Assert.Contains("LARANET005", codes);
    }

    [Fact]
    public void Doctor_ShouldReportDuplicateTopLevelTypes()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(
            directory.GetPath("One.cs"),
            "namespace Sample;\npublic sealed class Duplicate;\n");
        File.WriteAllText(
            directory.GetPath("Two.cs"),
            "namespace Sample;\npublic sealed class Duplicate;\n");

        var result = Run("doctor", "--path", directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("LARANET006", result.Output);
        Assert.Contains("Duplicate", result.Output);
    }

    [Fact]
    public void Doctor_ShouldEnableHeuristicsAndStrictWarnings()
    {
        using var directory = new TemporaryDirectory();
        Directory.CreateDirectory(directory.GetPath("Models"));
        Directory.CreateDirectory(directory.GetPath("Controllers"));
        Directory.CreateDirectory(directory.GetPath("Contracts"));
        File.WriteAllText(
            directory.GetPath("Models", "Product.cs"),
            "namespace Sample.Models;\npublic sealed class Product;\n");
        File.WriteAllText(
            directory.GetPath("Contracts", "ProductRequest.cs"),
            """
            namespace Sample.Contracts;
            public sealed class ProductRequest
            {
                public Product Product { get; init; }
            }
            """);
        File.WriteAllText(
            directory.GetPath("Controllers", "ProductsController.cs"),
            """
            using Microsoft.EntityFrameworkCore;
            public sealed class ProductsController(DbContext context) { }
            """);
        File.WriteAllText(
            directory.GetPath("Program.cs"),
            """
            app.MapGet("/products", async (CatalogDbContext db) =>
                await db.Products.ToListAsync());
            """);

        var result = Run(
            "doctor",
            "--path",
            directory.Root,
            "--check-pagination",
            "--check-controller-dbcontext",
            "--check-dto-entities",
            "--strict");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("LARANET007", result.Output);
        Assert.Contains("LARANET008", result.Output);
        Assert.Contains("LARANET009", result.Output);
        Assert.Contains("warning", result.Output);
    }

    [Fact]
    public void Doctor_ShouldReturnWarningsWithoutFailingByDefault()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(
            directory.GetPath("Program.cs"),
            "app.MapGet(\"/items\", () => items.ToList());\n");

        var result = Run("doctor", "--path", directory.Root, "--check-pagination");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("warning LARANET007", result.Output);
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
