using Laranet.Cli;
using Laranet.Cli.Generation;

namespace Laranet.Tests.Unit.Cli;

public class ProjectTemplateTests
{
    [Fact]
    public void Parse_ShouldDefaultToMinimalApi()
    {
        Assert.Equal(ProjectTemplate.MinimalApi, ProjectTemplates.Parse(null));
        Assert.Equal(ProjectTemplate.MinimalApi, ProjectTemplates.Parse("   "));
    }

    [Theory]
    [InlineData("minimal-api", "MinimalApi")]
    [InlineData("minimal", "MinimalApi")]
    [InlineData("webapi", "MinimalApi")]
    [InlineData("web-api", "MinimalApi")]
    [InlineData("mvc", "Mvc")]
    [InlineData("layered", "Layered")]
    [InlineData("hexagonal", "Hexagonal")]
    [InlineData("modular", "Modular")]
    [InlineData("console", "Console")]
    [InlineData("  MVC  ", "Mvc")]
    public void Parse_ShouldAcceptEveryTemplateName(
        string value,
        string expected)
    {
        Assert.Equal(
            Enum.Parse<ProjectTemplate>(expected),
            ProjectTemplates.Parse(value));
    }

    [Fact]
    public void Parse_ShouldRejectUnknownTemplate()
    {
        var exception = Assert.Throws<CliException>(
            () => ProjectTemplates.Parse("blazor"));

        Assert.Contains("not a known template", exception.Message);
        Assert.Contains("minimal-api", exception.Message);
        Assert.Contains("console", exception.Message);
    }

    [Theory]
    [InlineData("minimal-api", false)]
    [InlineData("mvc", false)]
    [InlineData("modular", false)]
    [InlineData("console", false)]
    [InlineData("layered", true)]
    [InlineData("hexagonal", true)]
    public void IsMultiProject_ShouldOnlyAcceptArchitecturalTemplates(
        string templateName,
        bool expected)
    {
        Assert.Equal(
            expected,
            ProjectTemplates.IsMultiProject(ProjectTemplates.Parse(templateName)));
    }

    [Fact]
    public void Layers_ShouldReferenceOnlyEarlierProjects()
    {
        var layers = ProjectTemplates.Layers(
            ProjectTemplate.Layered,
            "my-api",
            "MyApi");

        Assert.Equal(4, layers.Count);

        for (var index = 0; index < layers.Count; index++)
        {
            var available = layers
                .Take(index)
                .Select(layer => layer.FolderName)
                .ToHashSet(StringComparer.Ordinal);

            Assert.All(
                layers[index].ProjectReferences,
                reference => Assert.Contains(reference, available));
        }
    }
}
