using Laranet.Cli;
using Laranet.Cli.Generation;

namespace Laranet.Tests.Unit.Cli;

public class CrudProfileParserTests
{
    [Theory]
    [InlineData(null, "Simple")]
    [InlineData("", "Simple")]
    [InlineData("simple", "Simple")]
    [InlineData("STANDARD", "Standard")]
    [InlineData("clean", "Clean")]
    [InlineData("modular", "Modular")]
    public void Parse_ShouldReturnExpectedProfile(
        string? value,
        string expected)
    {
        Assert.Equal(expected, CrudProfileParser.Parse(value).ToString());
    }

    [Fact]
    public void Parse_ShouldThrow_WhenProfileIsUnknown()
    {
        var exception = Assert.Throws<CliException>(
            () => CrudProfileParser.Parse("microservices"));

        Assert.Contains("simple, standard, clean, modular", exception.Message);
    }

    [Theory]
    [InlineData("simple", "simple")]
    [InlineData("standard", "standard")]
    [InlineData("clean", "clean")]
    [InlineData("modular", "modular")]
    public void Name_ShouldReturnTheCommandLineName(
        string profile,
        string expected)
    {
        Assert.Equal(
            expected,
            CrudProfileParser.Name(CrudProfileParser.Parse(profile)));
    }
}
