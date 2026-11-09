using Laranet.Cli;
using Laranet.Tests.TestSupport;

namespace Laranet.Tests.Integration.Cli;

public class ProjectStructureTests
{
    [Fact]
    public void New_ShouldCreateSolution_WhenRequested()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-api",
            "--sln",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);

        var solution = directory.GetPath("my-api.slnx");
        Assert.True(File.Exists(solution));
        Assert.Contains(
            "<Project Path=\"my-api/my-api.csproj\" />",
            File.ReadAllText(solution));
        Assert.True(File.Exists(directory.GetPath("my-api", "my-api.csproj")));
    }

    [Fact]
    public void New_ShouldNotCreateSolutionByDefault()
    {
        using var directory = new TemporaryDirectory();

        Run("new", "my-api", "--output", directory.Root);

        Assert.False(File.Exists(directory.GetPath("my-api.slnx")));
        Assert.False(File.Exists(directory.GetPath("my-api", "my-api.slnx")));
    }

    [Fact]
    public void New_ShouldCreateRepositoryLayout()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-api",
            "--layout",
            "repository",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);

        var repository = directory.GetPath("my-api");
        var project = Path.Combine(repository, "src", "my-api", "my-api.csproj");

        Assert.True(File.Exists(project));
        Assert.True(File.Exists(Path.Combine(repository, "src", "my-api", "Program.cs")));
        Assert.True(File.Exists(Path.Combine(repository, "src", "my-api", "appsettings.json")));
        Assert.True(File.Exists(Path.Combine(repository, ".gitignore")));
        Assert.True(File.Exists(Path.Combine(repository, "README.md")));
        Assert.False(File.Exists(Path.Combine(repository, "src", "my-api", ".gitignore")));

        var solution = Path.Combine(repository, "my-api.slnx");
        Assert.True(File.Exists(solution));
        Assert.Contains(
            "<Project Path=\"src/my-api/my-api.csproj\" />",
            File.ReadAllText(solution));

        Assert.Contains(
            "dotnet run --project src/my-api/my-api.csproj",
            File.ReadAllText(Path.Combine(repository, "README.md")));
    }

    [Fact]
    public void New_ShouldCreateRepositoryLayoutWithConsoleTemplate()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-tool",
            "--layout",
            "repository",
            "--template",
            "console",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);

        var projectDirectory = directory.GetPath("my-tool", "src", "my-tool");
        var project = File.ReadAllText(
            Path.Combine(projectDirectory, "my-tool.csproj"));

        Assert.Contains("<OutputType>Exe</OutputType>", project);
        Assert.False(File.Exists(Path.Combine(projectDirectory, "appsettings.json")));
        Assert.True(File.Exists(directory.GetPath("my-tool", "my-tool.slnx")));
    }

    [Fact]
    public void New_ShouldPrintRepositoryNextSteps()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-api",
            "--layout",
            "repository",
            "--output",
            directory.Root);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(
            "dotnet run --project src/my-api/my-api.csproj",
            result.Output);
    }

    [Fact]
    public void New_ShouldRejectUnknownLayout()
    {
        using var directory = new TemporaryDirectory();

        var result = Run(
            "new",
            "my-api",
            "--layout",
            "microservices",
            "--output",
            directory.Root);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("not a known layout", result.Error);
        Assert.False(Directory.Exists(directory.GetPath("my-api")));
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
