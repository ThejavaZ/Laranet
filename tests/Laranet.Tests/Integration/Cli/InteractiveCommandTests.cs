using Laranet.Cli;
using Laranet.Tests.TestSupport;

namespace Laranet.Tests.Integration.Cli;

public class InteractiveCommandTests
{
    [Fact]
    public void New_ShouldPromptForMissingValues()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "my-api\nconsole\nproject\nn\nn\nn\n",
            "new",
            "--interactive",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Project name:", result.Output);
        Assert.Contains("Template [minimal-api/mvc/layered/hexagonal/modular/console]", result.Output);

        var projectDirectory = directory.GetPath("my-api");
        var project = File.ReadAllText(Path.Combine(projectDirectory, "my-api.csproj"));

        Assert.Contains("<OutputType>Exe</OutputType>", project);
        Assert.False(File.Exists(Path.Combine(projectDirectory, "appsettings.json")));
        Assert.DoesNotContain("PackageReference", project);
    }

    [Fact]
    public void New_ShouldNotPromptForProvidedValues()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "n\nn\nn\n",
            "new",
            "my-api",
            "--template",
            "console",
            "--layout",
            "project",
            "--interactive",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("Project name:", result.Output);
        Assert.DoesNotContain("Template [minimal-api/mvc/layered/hexagonal/modular/console]", result.Output);
        Assert.True(File.Exists(directory.GetPath("my-api", "my-api.csproj")));
    }

    [Fact]
    public void New_ShouldPromptForLayout()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "my-api\nconsole\nrepository\nn\nn\nn\n",
            "new",
            "--interactive",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Layout [project/repository]", result.Output);
        Assert.True(File.Exists(directory.GetPath("my-api", "src", "my-api", "my-api.csproj")));
        Assert.True(File.Exists(directory.GetPath("my-api", "my-api.slnx")));
    }

    [Fact]
    public void New_ShouldSkipPackagePrompts_WhenAPackageFlagIsProvided()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            string.Empty,
            "new",
            "my-api",
            "--template",
            "console",
            "--layout",
            "project",
            "--with-data",
            "--interactive",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("Reference Laranet.Data?", result.Output);

        var project = File.ReadAllText(directory.GetPath("my-api", "my-api.csproj"));

        Assert.Contains("Include=\"Laranet.Data\"", project);
        Assert.DoesNotContain("Laranet.EntityFrameworkCore", project);
        Assert.DoesNotContain("Laranet.AspNetCore", project);
    }

    [Fact]
    public void New_ShouldWriteIntoNonEmptyDirectory_WhenConfirmed()
    {
        using var directory = new TemporaryDirectory();
        var projectDirectory = Directory.CreateDirectory(directory.GetPath("my-api")).FullName;
        var existingFile = Path.Combine(projectDirectory, "notes.txt");
        File.WriteAllText(existingFile, "keep me");

        var result = Run(
            "n\nn\nn\ny\n",
            "new",
            "my-api",
            "--template",
            "console",
            "--layout",
            "project",
            "--interactive",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(projectDirectory, "my-api.csproj")));
        Assert.Equal("keep me", File.ReadAllText(existingFile));
    }

    [Fact]
    public void New_ShouldCancel_WhenNonEmptyDirectoryIsDeclined()
    {
        using var directory = new TemporaryDirectory();
        var projectDirectory = Directory.CreateDirectory(directory.GetPath("my-api")).FullName;
        File.WriteAllText(Path.Combine(projectDirectory, "notes.txt"), "keep me");

        var result = Run(
            "n\nn\nn\nn\n",
            "new",
            "my-api",
            "--template",
            "console",
            "--layout",
            "project",
            "--interactive",
            "--output",
            directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("cancelled", result.Error);
        Assert.False(File.Exists(Path.Combine(projectDirectory, "my-api.csproj")));
    }

    [Fact]
    public void New_ShouldReprompt_WhenNameIsInvalid()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "../evil\nmy-api\n",
            "new",
            "--template",
            "console",
            "--layout",
            "project",
            "--with-data",
            "--interactive",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("not a valid project name", result.Output);
        Assert.True(File.Exists(directory.GetPath("my-api", "my-api.csproj")));
    }

    [Fact]
    public void MakeCommand_ShouldPromptForMissingName()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "product\n",
            "make:model",
            "--namespace",
            "Test.App",
            "--interactive",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Name:", result.Output);
        Assert.True(File.Exists(directory.GetPath("Models", "Product.cs")));
    }

    [Fact]
    public void MakeCommand_ShouldReprompt_WhenNameIsInvalid()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "123product\nproduct\n",
            "make:model",
            "--namespace",
            "Test.App",
            "--interactive",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("not a valid name", result.Output);
        Assert.True(File.Exists(directory.GetPath("Models", "Product.cs")));
    }

    [Fact]
    public void MakeCommand_ShouldPromptForNamespace_WhenItCannotBeInferred()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "Test.App\n",
            "make:model",
            "product",
            "--interactive",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Namespace:", result.Output);
        Assert.Contains(
            "namespace Test.App.Models;",
            File.ReadAllText(directory.GetPath("Models", "Product.cs")));
    }

    [Fact]
    public void MakeCommand_ShouldReprompt_WhenNamespaceIsInvalid()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "9Invalid\nTest.App\n",
            "make:model",
            "product",
            "--interactive",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("not a valid namespace", result.Output);
        Assert.True(File.Exists(directory.GetPath("Models", "Product.cs")));
    }

    [Fact]
    public void MakeCommand_ShouldOverwrite_WhenConfirmed()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.GetPath("Models", "Product.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "custom content");

        var result = Run(
            "y\n",
            "make:model",
            "product",
            "--namespace",
            "Test.App",
            "--interactive",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Overwrite them?", result.Output);
        Assert.Contains("namespace Test.App.Models;", File.ReadAllText(path));
    }

    [Fact]
    public void MakeCommand_ShouldNotOverwrite_WhenDeclined()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.GetPath("Models", "Product.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "custom content");

        var result = Run(
            "n\n",
            "make:model",
            "product",
            "--namespace",
            "Test.App",
            "--interactive",
            "--output",
            directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("cancelled", result.Error);
        Assert.Equal("custom content", File.ReadAllText(path));
    }

    [Fact]
    public void Interactive_ShouldFail_WhenInputEndsUnexpectedly()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            string.Empty,
            "new",
            "--interactive",
            "--output",
            directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Interactive input ended unexpectedly", result.Error);
    }

    private static CommandResult Run(string input, params string[] args)
    {
        using var reader = new StringReader(input);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = CliApplication.Execute(args, output, error, reader);

        return new CommandResult(exitCode, output.ToString(), error.ToString());
    }

    private sealed record CommandResult(int ExitCode, string Output, string Error);
}
