using Laranet.Cli.Generation;
using Laranet.Tests.TestSupport;

namespace Laranet.Tests.Unit.Cli;

public class GeneratorContractTests
{
    [Fact]
    public void ModelGenerator_ShouldPlanConventionFolderAndNamespace()
    {
        using var directory = new TemporaryDirectory();
        var context = CreateContext(directory);

        var plan = new ModelGenerator().Create(context);

        var file = Assert.Single(plan.Files);
        Assert.Equal(directory.GetPath("Models", "Product.cs"), file.Path);
        Assert.Contains("namespace Test.App.Models;", file.Content);
        Assert.Contains("public class Product", file.Content);
    }

    [Fact]
    public void ModelGenerator_ShouldUseBaseNamespace_WhenFlat()
    {
        using var directory = new TemporaryDirectory();
        var context = CreateContext(directory, flat: true);

        var file = Assert.Single(new ModelGenerator().Create(context).Files);

        Assert.Equal(directory.GetPath("Product.cs"), file.Path);
        Assert.Contains("namespace Test.App;", file.Content);
        Assert.DoesNotContain("Test.App.Models", file.Content);
    }

    [Theory]
    [InlineData("Product", "ProductService")]
    [InlineData("ProductService", "ProductService")]
    public void ServiceGenerator_ShouldAppendSuffixOnlyWhenMissing(
        string name,
        string expectedTypeName)
    {
        using var directory = new TemporaryDirectory();
        var context = CreateContext(directory, name);

        var file = Assert.Single(new ServiceGenerator().Create(context).Files);

        Assert.Equal(
            directory.GetPath("Services", $"{expectedTypeName}.cs"),
            file.Path);
        Assert.Contains($"public sealed class {expectedTypeName}", file.Content);
    }

    [Fact]
    public void ResourceGenerator_ShouldComposeModelServiceAndController()
    {
        using var directory = new TemporaryDirectory();
        var context = CreateContext(directory);

        var plan = new ResourceGenerator().Create(context);

        Assert.Equal(3, plan.Files.Count);
        Assert.Contains(
            plan.Files,
            file => file.Path == directory.GetPath("Models", "Product.cs"));
        Assert.Contains(
            plan.Files,
            file => file.Path == directory.GetPath("Services", "ProductService.cs"));
        Assert.Contains(
            plan.Files,
            file => file.Path == directory.GetPath("Controllers", "ProductController.cs"));
    }

    [Fact]
    public void ResourceGenerator_ShouldNotIncludeOptionalGenerators()
    {
        using var directory = new TemporaryDirectory();
        var context = CreateContext(directory);

        var plan = new ResourceGenerator().Create(context);

        Assert.DoesNotContain(
            plan.Files,
            file => file.Path.Contains("Repositories", StringComparison.Ordinal));
        Assert.DoesNotContain(
            plan.Files,
            file => file.Path.Contains("Middleware", StringComparison.Ordinal));
    }

    [Fact]
    public void Generator_ShouldUseTemplateOverride()
    {
        using var directory = new TemporaryDirectory();
        var templates = CreateTemplatesDirectory(directory.Root);
        File.WriteAllText(
            Path.Combine(templates, "model.tpl"),
            "namespace {{Namespace}};\n\npublic sealed record {{Name}}(int Id);\n");

        var context = CreateContext(directory);

        var file = Assert.Single(new ModelGenerator().Create(context).Files);

        Assert.Equal(
            "namespace Test.App.Models;\n\npublic sealed record Product(int Id);\n",
            file.Content);
    }

    [Fact]
    public void GeneratorRegistry_ShouldResolveEveryKind()
    {
        foreach (var kind in Enum.GetValues<GeneratorKind>())
        {
            Assert.NotNull(GeneratorRegistry.Resolve(kind));
        }
    }

    [Fact]
    public void GeneratorRegistry_ShouldThrow_WhenKindIsUnsupported()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GeneratorRegistry.Resolve((GeneratorKind)999));
    }

    private static GenerationContext CreateContext(
        TemporaryDirectory directory,
        string name = "Product",
        bool flat = false)
    {
        return new GenerationContext(
            name,
            "Test.App",
            directory.Root,
            flat,
            TemplateCatalog.Discover(directory.Root));
    }

    private static string CreateTemplatesDirectory(string root)
    {
        return Directory.CreateDirectory(
            Path.Combine(root, ".laranet", "templates")).FullName;
    }
}
