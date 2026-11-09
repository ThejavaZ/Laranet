using Laranet.Cli;
using Laranet.Cli.Generation;

namespace Laranet.Tests.Unit.Cli;

public class ProjectLayoutParserTests
{
    [Theory]
    [InlineData(null, "Project")]
    [InlineData("", "Project")]
    [InlineData("project", "Project")]
    [InlineData("PROJECT", "Project")]
    [InlineData("repository", "Repository")]
    [InlineData("repo", "Repository")]
    public void Parse_ShouldReturnExpectedLayout(
        string? input,
        string expected)
    {
        Assert.Equal(expected, ProjectLayoutParser.Parse(input).ToString());
    }

    [Theory]
    [InlineData("microservices")]
    [InlineData("solution")]
    public void Parse_ShouldThrow_WhenLayoutIsUnknown(string input)
    {
        Assert.Throws<CliException>(() => ProjectLayoutParser.Parse(input));
    }
}
