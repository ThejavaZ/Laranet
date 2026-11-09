using System.Linq.Expressions;
using Laranet.Data.Extensions;

namespace Laranet.Tests.Unit.Data;

public class SearchTests
{
    private sealed record Product(string? Name);

    private sealed record Article(string? Title, string? Body);

    private static IQueryable<Product> Products =>
        new[]
        {
            new Product("Mechanical Keyboard"),
            new Product("Gaming Mouse"),
            new Product("Wireless Keyboard"),
            new Product(null)
        }.AsQueryable();

    private static IQueryable<Article> Articles =>
        new[]
        {
            new Article("Mechanical Keyboard", "Wired"),
            new Article("Gaming Mouse", "Mechanical Keyboard switches"),
            new Article("Monitor", null),
            new Article(null, null)
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

    [Fact]
    public void Search_ShouldThrow_WhenSourceIsNull()
    {
        IQueryable<Product> source = null!;

        Assert.Throws<ArgumentNullException>(() =>
            source.Search(product => product.Name, "Keyboard")
        );
    }

    [Fact]
    public void Search_ShouldThrow_WhenSelectorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Products.Search<Product>(
                (Expression<Func<Product, string?>>)null!,
                "Keyboard")
        );
    }

    [Fact]
    public void Search_ShouldComposeWithExistingFilters()
    {
        var result = Products
            .Where(product => product.Name != null)
            .Search(product => product.Name, "Keyboard");

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Search_ShouldMatchAnyOfSeveralSelectors()
    {
        var result = Articles.Search(
            [article => article.Title, article => article.Body],
            "Keyboard");

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Search_ShouldReturnOriginalQuery_WhenTermIsEmptyWithSeveralSelectors()
    {
        var query = Articles;

        var result = query.Search(
            [article => article.Title, article => article.Body],
            "   ");

        Assert.Same(query, result);
    }

    [Fact]
    public void Search_ShouldIgnoreNullColumns()
    {
        var result = Articles.Search(
            [article => article.Title, article => article.Body],
            "Keyboard");

        Assert.DoesNotContain(result, article => article.Title is null && article.Body is null);
    }

    [Fact]
    public void Search_ShouldThrow_WhenSelectorsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Articles.Search(
                (IEnumerable<Expression<Func<Article, string?>>>)null!,
                "Keyboard")
        );
    }

    [Fact]
    public void Search_ShouldThrow_WhenSelectorsIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            Articles.Search(
                Array.Empty<Expression<Func<Article, string?>>>(),
                "Keyboard")
        );
    }

    [Fact]
    public void Search_ShouldThrow_WhenAnySelectorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Articles.Search(
                [article => article.Title, null!],
                "Keyboard")
        );
    }
}
