using Laranet.Cli;
using Laranet.Cli.Generation;
using Laranet.Tests.TestSupport;

namespace Laranet.Tests.Unit.Cli;

public class NamespaceResolverTests
{
    [Fact]
    public void Resolve_ShouldUseExplicitNamespace()
    {
        using var directory = new TemporaryDirectory();

        var result = NamespaceResolver.Resolve(directory.Root, "Custom.App");

        Assert.Equal("Custom.App", result);
    }

    [Fact]
    public void Resolve_ShouldUseRootNamespaceFromProjectFile()
    {
        using var directory = new TemporaryDirectory();
        WriteProjectFile(directory.Root, "Test.csproj", "Smoke.App");

        var result = NamespaceResolver.Resolve(directory.Root, null);

        Assert.Equal("Smoke.App", result);
    }

    [Fact]
    public void Resolve_ShouldUseAssemblyName_WhenRootNamespaceIsMissing()
    {
        using var directory = new TemporaryDirectory();
        WriteFile(
            directory.Root,
            "Test.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <AssemblyName>Assembly.App</AssemblyName>
              </PropertyGroup>
            </Project>
            """);

        var result = NamespaceResolver.Resolve(directory.Root, null);

        Assert.Equal("Assembly.App", result);
    }

    [Fact]
    public void Resolve_ShouldUseProjectFileName_WhenProjectHasNoNamespaceMetadata()
    {
        using var directory = new TemporaryDirectory();
        WriteFile(
            directory.Root,
            "sample-app.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk" />
            """);

        var result = NamespaceResolver.Resolve(directory.Root, null);

        Assert.Equal("sample_app", result);
    }

    [Fact]
    public void Resolve_ShouldFindProjectFileInParentDirectory()
    {
        using var directory = new TemporaryDirectory();
        WriteProjectFile(directory.Root, "Test.csproj", "Parent.App");

        var nested = Directory.CreateDirectory(directory.GetPath("src", "Feature")).FullName;

        var result = NamespaceResolver.Resolve(nested, null);

        Assert.Equal("Parent.App", result);
    }

    [Fact]
    public void Resolve_ShouldFindProjectFile_WhenOutputDirectoryDoesNotExist()
    {
        using var directory = new TemporaryDirectory();
        WriteProjectFile(directory.Root, "Test.csproj", "Parent.App");

        var missing = directory.GetPath("src", "Feature");

        var result = NamespaceResolver.Resolve(missing, null);

        Assert.Equal("Parent.App", result);
    }

    [Fact]
    public void Resolve_ShouldSanitizeProjectNamespace()
    {
        using var directory = new TemporaryDirectory();
        WriteProjectFile(directory.Root, "Test.csproj", "9-Invalid.App");

        var result = NamespaceResolver.Resolve(directory.Root, null);

        Assert.Equal("_9_Invalid.App", result);
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenNoProjectFileExists()
    {
        using var directory = new TemporaryDirectory();

        Assert.Throws<CliException>(() =>
            NamespaceResolver.Resolve(directory.Root, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("9App")]
    [InlineData("App..Feature")]
    [InlineData("App.Feature-Name")]
    public void Resolve_ShouldThrow_WhenExplicitNamespaceIsInvalid(string value)
    {
        using var directory = new TemporaryDirectory();

        Assert.Throws<CliException>(() =>
            NamespaceResolver.Resolve(directory.Root, value));
    }

    private static void WriteProjectFile(
        string directory,
        string fileName,
        string rootNamespace)
    {
        WriteFile(
            directory,
            fileName,
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <RootNamespace>{rootNamespace}</RootNamespace>
              </PropertyGroup>
            </Project>
            """);
    }

    private static void WriteFile(
        string directory,
        string fileName,
        string content)
    {
        File.WriteAllText(Path.Combine(directory, fileName), content);
    }
}
