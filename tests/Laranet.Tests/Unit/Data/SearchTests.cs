using Laranet.Data.Extensions;

namespace Laranet.Tests.Unit.Data;

public class SearchTests
{
    private sealed record Product(string? Name);

    private static IQueryable<Product> Products =>
        new[]
        {
            new Product("Mechanical Keyboard"),
            new Product("Gaming Mouse"),
            new Product("Wireless Keyboard"),
            new Product(null)
        }.AsQueryable();

    [Fact]
    public void Search_ShouldReturnMatchingItems()
    {
        var result = Products.Search(
            product => product.Name,
            "Keyboard"
        );

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Search_ShouldReturnEmpty_WhenNoMatchExists()
    {
        var result = Products.Search(
            product => product.Name,
            "Monitor"
        );

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Search_ShouldReturnOriginalQuery_WhenTermIsEmpty(
        string? term)
    {
        var query = Products;

        var result = query.Search(
            product => product.Name,
            term
        );

        Assert.Same(query, result);
    }

    [Fact]
    public void Search_ShouldIgnoreNullProperties()
    {
        var result = Products.Search(
            product => product.Name,
            "Keyboard"
        );

        Assert.DoesNotContain(
            result,
            product => product.Name is null
        );
    }
}
