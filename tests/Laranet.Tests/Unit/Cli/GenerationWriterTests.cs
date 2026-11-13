using Laranet.Cli.Generation;
using Laranet.Tests.TestSupport;

namespace Laranet.Tests.Unit.Cli;

public class GenerationWriterTests
{
    [Fact]
    public void Write_ShouldCreateDirectoriesAndFiles()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.GetPath("Models", "Product.cs");
        var plan = CreatePlan(path, "generated");

        var exitCode = Write(
            directory,
            plan,
            new GenerationOptions(),
            out var output,
            out var error);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(path));
        Assert.Equal("generated", File.ReadAllText(path));
        Assert.Contains("created", output);
        Assert.Empty(error);
    }

    [Fact]
    public void Write_ShouldReturnErrorAndKeepFile_WhenFileExistsWithoutForce()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.GetPath("Models", "Product.cs");
        CreateExistingFile(path, "custom");
        var plan = CreatePlan(path, "generated");

        var exitCode = Write(
            directory,
            plan,
            new GenerationOptions(),
            out _,
            out var error);

        Assert.Equal(1, exitCode);
        Assert.Equal("custom", File.ReadAllText(path));
        Assert.Contains("already exists", error);
    }

    [Fact]
    public void Write_ShouldNotWriteAnyFile_WhenOneFileConflicts()
    {
        using var directory = new TemporaryDirectory();
        var conflictPath = directory.GetPath("Models", "Product.cs");
        var newPath = directory.GetPath("Services", "ProductService.cs");
        CreateExistingFile(conflictPath, "custom");

        var plan = new GenerationPlan("Product", [
            new GeneratedFile(conflictPath, "generated model"),
            new GeneratedFile(newPath, "generated service"),
        ]);

        var exitCode = Write(
            directory,
            plan,
            new GenerationOptions(),
            out _,
            out _);

        Assert.Equal(1, exitCode);
        Assert.Equal("custom", File.ReadAllText(conflictPath));
        Assert.False(File.Exists(newPath));
    }

    [Fact]
    public void Write_ShouldOverwrite_WhenForceIsTrue()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.GetPath("Models", "Product.cs");
        CreateExistingFile(path, "custom");
        var plan = CreatePlan(path, "generated");

        var exitCode = Write(
            directory,
            plan,
            new GenerationOptions(Force: true),
            out var output,
            out _);

        Assert.Equal(0, exitCode);
        Assert.Equal("generated", File.ReadAllText(path));
        Assert.Contains("overwrote", output);
    }

    [Fact]
    public void Write_ShouldReportWithoutWriting_WhenDryRunIsTrue()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.GetPath("Models", "Product.cs");
        var plan = CreatePlan(path, "generated");

        var exitCode = Write(
            directory,
            plan,
            new GenerationOptions(DryRun: true),
            out var output,
            out _);

        Assert.Equal(0, exitCode);
        Assert.False(File.Exists(path));
        Assert.Contains("would create", output);
    }

    private static GenerationPlan CreatePlan(string path, string content)
    {
        return new GenerationPlan("Product", [
            new GeneratedFile(path, content),
        ]);
    }

    private static void CreateExistingFile(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static int Write(
        TemporaryDirectory directory,
        GenerationPlan plan,
        GenerationOptions options,
        out string output,
        out string error)
    {
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        var exitCode = new GenerationWriter(
            outputWriter,
            errorWriter,
            directory.Root).Write(plan, options);

        output = outputWriter.ToString();
        error = errorWriter.ToString();

        return exitCode;
    }
}
