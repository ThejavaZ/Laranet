using Laranet.Data.Extensions;
using Laranet.Data.Sorting;

namespace Laranet.Tests.Unit.Data;

public class DynamicSortTests
{
    private sealed record Product(string Name, decimal Price);

    private static IQueryable<Product> CreateQuery()
    {
        return new[]
        {
            new Product("Keyboard", 100),
            new Product("Mouse", 50),
            new Product("Monitor", 200),
            new Product("Headset", 75)
        }.AsQueryable();
    }

    [Fact]
    public void OrderBy_ShouldSortAscending()
    {
        var result = CreateQuery()
            .OrderBy("Name")
            .ToList();

        Assert.Equal(
            new[] { "Headset", "Keyboard", "Monitor", "Mouse" },
            result.Select(product => product.Name)
        );
    }

    [Fact]
    public void OrderBy_ShouldSortDescending()
    {
        var result = CreateQuery()
            .OrderBy("Price", SortDirection.Descending)
            .ToList();

        Assert.Equal(
            new[] { 200m, 100m, 75m, 50m },
            result.Select(product => product.Price)
        );
    }

    [Fact]
    public void ThenBy_ShouldApplySecondarySorting()
    {
        var products = new[]
        {
            new Product("Keyboard", 100),
            new Product("Keyboard", 50),
            new Product("Mouse", 75)
        }.AsQueryable();

        var result = products
            .OrderBy("Name")
            .ThenBy("Price", SortDirection.Descending)
            .ToList();

        Assert.Equal(
            new[] { 100m, 50m, 75m },
            result.Select(product => product.Price)
        );
    }

    [Fact]
    public void OrderBy_ShouldThrow_WhenPropertyDoesNotExist()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateQuery().OrderBy("Unknown")
        );
    }

    [Fact]
    public void OrderBy_ShouldThrow_WhenPropertyNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateQuery().OrderBy("")
        );
    }

    [Fact]
    public void OrderBy_ShouldThrow_WhenPropertyNameHasIncorrectCase()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateQuery().OrderBy("name")
        );
    }

    [Fact]
    public void OrderBy_ShouldThrow_WhenSourceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            QueryableDynamicSortExtensions.OrderBy<Product>(null!, "Name")
        );
    }

    [Fact]
    public void OrderBy_ShouldThrow_WhenDirectionIsInvalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateQuery().OrderBy("Name", (SortDirection)99)
        );
    }
}
