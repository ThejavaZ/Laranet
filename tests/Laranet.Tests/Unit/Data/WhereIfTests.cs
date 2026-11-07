using Laranet.Data.Extensions;

namespace Laranet.Tests.Unit.Data;

public class WhereIfTests
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
    public void WhereIf_ShouldApplyFilter_WhenConditionIsTrue()
    {
        var result = CreateQuery()
            .WhereIf(true, product => product.Price > 75)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, product => Assert.True(product.Price > 75));
    }

    [Fact]
    public void WhereIf_ShouldNotApplyFilter_WhenConditionIsFalse()
    {
        var source = CreateQuery();

        var result = source
            .WhereIf(false, product => product.Price > 100);

        Assert.Same(source, result);
        Assert.Equal(4, result.Count());
    }

    [Fact]
    public void WhereIf_ShouldSupportChainedConditions()
    {
        var result = CreateQuery()
            .WhereIf(true, product => product.Price >= 50)
            .WhereIf(true, product => product.Price <= 100)
            .WhereIf(false, product => product.Name == "Mouse")
            .ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void WhereIf_ShouldThrow_WhenSourceIsNull()
    {
        IQueryable<Product>? source = null;

        Assert.Throws<ArgumentNullException>(() =>
            source!.WhereIf(true, product => product.Price > 50));
    }

    [Fact]
    public void WhereIf_ShouldThrow_WhenPredicateIsNull()
    {
        var source = CreateQuery();

        Assert.Throws<ArgumentNullException>(() =>
            source.WhereIf(true, null!));
    }
}
