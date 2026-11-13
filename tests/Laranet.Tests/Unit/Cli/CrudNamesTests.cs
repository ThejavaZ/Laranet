using Laranet.Cli.Generation;

namespace Laranet.Tests.Unit.Cli;

public class CrudNamesTests
{
    [Theory]
    [InlineData("Product", "products")]
    [InlineData("Category", "categories")]
    [InlineData("Day", "days")]
    [InlineData("Address", "addresses")]
    [InlineData("Box", "boxes")]
    [InlineData("Church", "churches")]
    [InlineData("Dish", "dishes")]
    public void Pluralize_ShouldReturnTheRouteSegment(string name, string expected)
    {
        Assert.Equal(expected, CrudNames.Pluralize(name));
    }

    [Theory]
    [InlineData("Product", "product")]
    [InlineData("Event", "@event")]
    [InlineData("Class", "@class")]
    public void Variable_ShouldEscapeReservedKeywords(string name, string expected)
    {
        Assert.Equal(expected, CrudNames.Variable(name));
    }

    [Theory]
    [InlineData("Category", "Categories")]
    [InlineData("Product", "Products")]
    public void PluralizeType_ShouldReturnAPascalCasePlural(string name, string expected)
    {
        Assert.Equal(expected, CrudNames.PluralizeType(name));
    }
}
