using Laranet.Cli;
using Laranet.Tests.TestSupport;

namespace Laranet.Tests.Integration.Cli;

public class TemplateCustomizationTests
{
    [Fact]
    public void MakeModel_ShouldUseUserTemplate()
    {
        using var directory = new TemporaryDirectory();
        WriteTemplate(
            directory.Root,
            "model.tpl",
            """
            namespace {{Namespace}};

            public sealed record {{Name}}(int Id);

            """);

        var result = Run(
            "make:model",
            "product",
            "--output",
            directory.GetPath("src", "Feature"),
            "--namespace",
            "Test.App");

        Assert.Equal(0, result.ExitCode);

        var content = File.ReadAllText(
            directory.GetPath("src", "Feature", "Models", "Product.cs"));

        Assert.Equal(
            """
            namespace Test.App.Models;

            public sealed record Product(int Id);

            """,
            content);
    }

    [Fact]
    public void MakeCommand_ShouldUseBuiltInTemplate_WhenNoOverrideExists()
    {
        using var directory = new TemporaryDirectory();
        WriteTemplate(directory.Root, "model.tpl", "custom {{Name}}");

        var result = Run(
            "make:service",
            "product",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.Equal(0, result.ExitCode);

        var content = File.ReadAllText(directory.GetPath("Services", "ProductService.cs"));

        Assert.Contains("public sealed class ProductService", content);
        Assert.DoesNotContain("custom", content);
    }

    [Fact]
    public void MakeResource_ShouldApplyOverridesPerGenerator()
    {
        using var directory = new TemporaryDirectory();
        WriteTemplate(
            directory.Root,
            "model.tpl",
            """
            namespace {{Namespace}};

            public sealed record {{Name}}(int Id);

            """);
        WriteTemplate(
            directory.Root,
            "controller.tpl",
            """
            namespace {{Namespace}};

            public sealed class {{Name}};

            """);

        var result = Run(
            "make:resource",
            "product",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.Equal(0, result.ExitCode);

        Assert.Contains(
            "public sealed record Product(int Id);",
            File.ReadAllText(directory.GetPath("Models", "Product.cs")));
        Assert.Contains(
            "public sealed class ProductController;",
            File.ReadAllText(directory.GetPath("Controllers", "ProductController.cs")));
        Assert.Contains(
            "public sealed class ProductService",
            File.ReadAllText(directory.GetPath("Services", "ProductService.cs")));
    }

    [Fact]
    public void MakeCommand_ShouldLeaveUnknownPlaceholdersUntouched()
    {
        using var directory = new TemporaryDirectory();
        WriteTemplate(
            directory.Root,
            "model.tpl",
            "// {{Name}} in {{Namespace}} — {{Unknown}}");

        Run(
            "make:model",
            "product",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.Equal(
            "// Product in Test.App.Models — {{Unknown}}",
            File.ReadAllText(directory.GetPath("Models", "Product.cs")));
    }

    [Fact]
    public void MakeCommand_ShouldReturnError_WhenTemplateIsEmpty()
    {
        using var directory = new TemporaryDirectory();
        WriteTemplate(directory.Root, "model.tpl", "  \n");

        var result = Run(
            "make:model",
            "product",
            "--output",
            directory.Root,
            "--namespace",
            "Test.App");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("is empty", result.Error);
        Assert.False(Directory.Exists(directory.GetPath("Models")));
    }

    private static void WriteTemplate(
        string root,
        string fileName,
        string content)
    {
        var templates = Directory.CreateDirectory(
            Path.Combine(root, ".laranet", "templates")).FullName;

        File.WriteAllText(Path.Combine(templates, fileName), content);
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
