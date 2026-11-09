using Laranet.Cli;
using Laranet.Tests.TestSupport;

namespace Laranet.Tests.Integration.Cli;

public class NewProjectTests
{
    [Fact]
    public void New_ShouldCreateWebApiProjectByDefault()
    {
        using var directory = new TemporaryDirectory();

        var result = Run("new", "my-api", "--output", directory.Root);

        Assert.Equal(0, result.ExitCode);

        var projectDirectory = directory.GetPath("my-api");
        var projectFile = Path.Combine(projectDirectory, "my-api.csproj");

        Assert.True(File.Exists(projectFile));
        Assert.True(File.Exists(Path.Combine(projectDirectory, "appsettings.json")));
        Assert.True(File.Exists(Path.Combine(projectDirectory, "appsettings.Development.json")));
        Assert.True(File.Exists(Path.Combine(projectDirectory, "Properties", "launchSettings.json")));
        Assert.True(File.Exists(Path.Combine(projectDirectory, ".gitignore")));
        Assert.True(File.Exists(Path.Combine(projectDirectory, "README.md")));

        var project = File.ReadAllText(projectFile);
        Assert.Contains("<Project Sdk=\"Microsoft.NET.Sdk.Web\">", project);
        Assert.Contains("<TargetFramework>net10.0</TargetFramework>", project);
        Assert.Contains("<RootNamespace>MyApi</RootNamespace>", project);

        Assert.Contains(
            "WebApplication.CreateBuilder",
            File.ReadAllText(Path.Combine(projectDirectory, "Program.cs")));
    }

    [Fact]
    public void New_ShouldCreateConsoleProject_WhenTemplateIsConsole()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-console",
            "--template",
            "console",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);

        var projectDirectory = directory.GetPath("my-console");
        var project = File.ReadAllText(
            Path.Combine(projectDirectory, "my-console.csproj"));

        Assert.Contains("<Project Sdk=\"Microsoft.NET.Sdk\">", project);
        Assert.Contains("<OutputType>Exe</OutputType>", project);
        Assert.Contains("<RootNamespace>MyConsole</RootNamespace>", project);
        Assert.False(File.Exists(Path.Combine(projectDirectory, "appsettings.json")));
        Assert.False(Directory.Exists(Path.Combine(projectDirectory, "Properties")));

        Assert.Contains(
            "Console.WriteLine(\"MyConsole is ready.\");",
            File.ReadAllText(Path.Combine(projectDirectory, "Program.cs")));
    }

    [Fact]
    public void New_ShouldUseExplicitNamespace()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-api",
            "--namespace",
            "Contoso.Billing",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(
            "<RootNamespace>Contoso.Billing</RootNamespace>",
            File.ReadAllText(directory.GetPath("my-api", "my-api.csproj")));
    }

    [Fact]
    public void New_ShouldAddRequestedPackageReferences()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-api",
            "--with-data",
            "--with-efcore",
            "--with-aspnetcore",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);

        var project = File.ReadAllText(directory.GetPath("my-api", "my-api.csproj"));

        Assert.Contains(
            $"<PackageReference Include=\"Laranet.Data\" Version=\"{LaranetVersion.Current}\" />",
            project);
        Assert.Contains(
            $"<PackageReference Include=\"Laranet.EntityFrameworkCore\" Version=\"{LaranetVersion.Current}\" />",
            project);
        Assert.Contains(
            $"<PackageReference Include=\"Laranet.AspNetCore\" Version=\"{LaranetVersion.Current}\" />",
            project);
    }

    [Fact]
    public void New_ShouldNotAddPackageReferences_WhenNoneRequested()
    {
        using var directory = new TemporaryDirectory();

        Run("new", "my-api", "--output", directory.Root);

        var project = File.ReadAllText(directory.GetPath("my-api", "my-api.csproj"));

        Assert.DoesNotContain("PackageReference", project);
    }

    [Fact]
    public void New_ShouldCreateProjectInsideOutputDirectory()
    {
        using var directory = new TemporaryDirectory();

        var output = directory.GetPath("src");

        var result = Run("new", "my-api", "--output", output);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(output, "my-api", "my-api.csproj")));
    }

    [Fact]
    public void New_ShouldRefuseNonEmptyDirectory()
    {
        using var directory = new TemporaryDirectory();
        var projectDirectory = Directory.CreateDirectory(directory.GetPath("my-api")).FullName;
        var existingFile = Path.Combine(projectDirectory, "notes.txt");
        File.WriteAllText(existingFile, "keep me");

        var result = Run("new", "my-api", "--output", directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("already exists and is not empty", result.Error);
        Assert.False(File.Exists(Path.Combine(projectDirectory, "my-api.csproj")));
        Assert.Equal("keep me", File.ReadAllText(existingFile));
    }

    [Fact]
    public void New_ShouldWriteIntoNonEmptyDirectory_WhenForceIsRequested()
    {
        using var directory = new TemporaryDirectory();
        var projectDirectory = Directory.CreateDirectory(directory.GetPath("my-api")).FullName;
        var existingFile = Path.Combine(projectDirectory, "notes.txt");
        File.WriteAllText(existingFile, "keep me");

        var result = Run("new", "my-api", "--output", directory.Root, "--force");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(projectDirectory, "my-api.csproj")));
        Assert.Equal("keep me", File.ReadAllText(existingFile));
    }

    [Fact]
    public void New_ShouldNotWriteFiles_WhenDryRunIsRequested()
    {
        using var directory = new TemporaryDirectory();

        var result = Run("new", "my-api", "--output", directory.Root, "--dry-run");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("would create", result.Output);
        Assert.False(Directory.Exists(directory.GetPath("my-api")));
    }

    [Fact]
    public void New_ShouldPrintNextSteps()
    {
        using var directory = new TemporaryDirectory();

        var result = Run("new", "my-api", "--output", directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Next steps:", result.Output);
        Assert.Contains("dotnet run", result.Output);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("../evil")]
    [InlineData("foo/bar")]
    [InlineData("foo\\bar")]
    public void New_ShouldRejectInvalidProjectName(string name)
    {
        using var directory = new TemporaryDirectory();

        var result = Run("new", name, "--output", directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("error:", result.Error);
    }

    [Fact]
    public void New_ShouldRejectUnknownTemplate()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-api",
            "--template",
            "blazor",
            "--output",
            directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("not a known template", result.Error);
        Assert.False(Directory.Exists(directory.GetPath("my-api")));
    }

    [Fact]
    public void New_ShouldRejectInvalidNamespace()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-api",
            "--namespace",
            "9Invalid",
            "--output",
            directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("not a valid namespace", result.Error);
    }

    [Fact]
    public void New_ShouldReturnError_WhenNameIsMissing()
    {
        using var directory = new TemporaryDirectory();

        var result = Run("new", "--output", directory.Root);

        Assert.NotEqual(0, result.ExitCode);
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
