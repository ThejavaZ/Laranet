using Laranet.Cli;
using Laranet.Cli.Generation;
using Laranet.Tests.TestSupport;

namespace Laranet.Tests.Unit.Cli;

public class TemplateCatalogTests
{
    [Fact]
    public void Find_ShouldReturnNull_WhenNoTemplatesDirectoryExists()
    {
        using var directory = new TemporaryDirectory();

        var catalog = TemplateCatalog.Discover(directory.Root);

        Assert.Null(catalog.Find(GeneratorKind.Model));
    }

    [Fact]
    public void Find_ShouldReturnUserTemplate()
    {
        using var directory = new TemporaryDirectory();
        var templates = CreateTemplatesDirectory(directory.Root);
        File.WriteAllText(Path.Combine(templates, "model.tpl"), "custom {{Name}}");

        var catalog = TemplateCatalog.Discover(directory.Root);

        Assert.Equal("custom {{Name}}", catalog.Find(GeneratorKind.Model));
    }

    [Fact]
    public void Find_ShouldWalkUpFromNestedDirectory()
    {
        using var directory = new TemporaryDirectory();
        var templates = CreateTemplatesDirectory(directory.Root);
        File.WriteAllText(Path.Combine(templates, "service.tpl"), "custom");

        var nested = directory.GetPath("src", "Feature");
        var catalog = TemplateCatalog.Discover(nested);

        Assert.Equal("custom", catalog.Find(GeneratorKind.Service));
    }

    [Fact]
    public void Find_ShouldUseNearestTemplatesDirectory()
    {
        using var directory = new TemporaryDirectory();
        var outer = CreateTemplatesDirectory(directory.Root);
        File.WriteAllText(Path.Combine(outer, "model.tpl"), "outer");

        var innerRoot = Directory.CreateDirectory(directory.GetPath("src")).FullName;
        var inner = CreateTemplatesDirectory(innerRoot);
        File.WriteAllText(Path.Combine(inner, "model.tpl"), "inner");

        var catalog = TemplateCatalog.Discover(directory.GetPath("src", "Feature"));

        Assert.Equal("inner", catalog.Find(GeneratorKind.Model));
    }

    [Fact]
    public void Find_ShouldIgnoreUnknownTemplateFiles()
    {
        using var directory = new TemporaryDirectory();
        var templates = CreateTemplatesDirectory(directory.Root);
        File.WriteAllText(Path.Combine(templates, "unrelated.tpl"), "ignored");

        var catalog = TemplateCatalog.Discover(directory.Root);

        Assert.Null(catalog.Find(GeneratorKind.Model));
    }

    [Fact]
    public void Find_ShouldThrow_WhenTemplateIsEmpty()
    {
        using var directory = new TemporaryDirectory();
        var templates = CreateTemplatesDirectory(directory.Root);
        File.WriteAllText(Path.Combine(templates, "model.tpl"), "   ");

        var catalog = TemplateCatalog.Discover(directory.Root);

        var exception = Assert.Throws<CliException>(
            () => catalog.Find(GeneratorKind.Model));

        Assert.Contains("is empty", exception.Message);
    }

    private static string CreateTemplatesDirectory(string root)
    {
        return Directory.CreateDirectory(
            Path.Combine(root, ".laranet", "templates")).FullName;
    }
}
