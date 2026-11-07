
using Laranet.Data.Extensions;
using Laranet.Data.Sorting;

namespace Laranet.Tests.Unit.Data;

public class SortByTests
{
    private sealed record Product(string Name, decimal Price);

    private static IQueryable<Product> Products =>
        new[]
        {
            new Product("Keyboard", 100),
            new Product("Mouse", 50),
            new Product("Monitor", 200),
            new Product("Headset", 75)
        }.AsQueryable();

    [Fact]
    public void SortBy_ShouldSortAscending_ByDefault()
    {
        var result = Products
            .SortBy(product => product.Price)
            .ToList();

        Assert.Equal(
            new[] { 50m, 75m, 100m, 200m },
            result.Select(product => product.Price)
        );
    }

    [Fact]
    public void SortBy_ShouldSortAscending_WhenDirectionIsAscending()
    {
        var result = Products
            .SortBy(product => product.Price, SortDirection.Ascending)
            .ToList();

        Assert.Equal(
            new[] { 50m, 75m, 100m, 200m },
            result.Select(product => product.Price)
        );
    }

    [Fact]
    public void SortBy_ShouldSortDescending_WhenDirectionIsDescending()
    {
        var result = Products
            .SortBy(product => product.Price, SortDirection.Descending)
            .ToList();

        Assert.Equal(
            new[] { 200m, 100m, 75m, 50m },
            result.Select(product => product.Price)
        );
    }

    [Fact]
    public void SortBy_ShouldThrow_WhenSourceIsNull()
    {
        IQueryable<Product> source = null!;

        Assert.Throws<ArgumentNullException>(() =>
            source.SortBy(product => product.Price)
        );
    }

    [Fact]
    public void SortBy_ShouldThrow_WhenSelectorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Products.SortBy<Product, decimal>(null!)
        );
    }

    [Fact]
    public void SortBy_ShouldThrow_WhenDirectionIsInvalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Products.SortBy(
                product => product.Price,
                (SortDirection)99
            )
        );
    }

    [Fact]
    public void SortBy_ShouldSupportThenBy()
    {
        var result = Products
            .SortBy(product => product.Price)
            .ThenBy(product => product.Name)
            .ToList();

        Assert.Equal(
            new[] { "Mouse", "Headset", "Keyboard", "Monitor" },
            result.Select(product => product.Name)
        );
    }
}
