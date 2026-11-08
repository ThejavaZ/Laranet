using Laranet.Data.Extensions;
using Laranet.Data.Sorting;
using Laranet.Tests.Integration.Data.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Data;

public class SortByIntegrationTests
{
    private static void SeedProducts(DataTestDbContext context)
    {
        context.Products.AddRange(
            new DataTestProduct { Name = "Keyboard" },
            new DataTestProduct { Name = "Mouse" },
            new DataTestProduct { Name = "Monitor" },
            new DataTestProduct { Name = "Keyboard" }
        );
    }

    [Fact]
    public async Task SortBy_ShouldTranslateAscendingToSql()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var query = database.Context.Products
            .SortBy(product => product.Name);

        var sql = query.ToQueryString();

        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);

        var results = await query
            .Select(product => product.Name)
            .ToListAsync();

        Assert.Equal(
            new[] { "Keyboard", "Keyboard", "Monitor", "Mouse" },
            results
        );
    }

    [Fact]
    public async Task SortBy_ShouldTranslateDescendingToSql()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var query = database.Context.Products
            .SortBy(product => product.Id, SortDirection.Descending);

        var sql = query.ToQueryString();

        Assert.Contains("DESC", sql, StringComparison.OrdinalIgnoreCase);

        var results = await query
            .Select(product => product.Id)
            .ToListAsync();

        Assert.Equal(new[] { 4, 3, 2, 1 }, results);
    }

    [Fact]
    public async Task SortBy_ShouldSupportThenByDescendingWithSqlite()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var query = database.Context.Products
            .SortBy(product => product.Name)
            .ThenByDescending(product => product.Id);

        var sql = query.ToQueryString();

        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DESC", sql, StringComparison.OrdinalIgnoreCase);

        var results = await query
            .Select(product => product.Id)
            .ToListAsync();

        Assert.Equal(new[] { 4, 1, 3, 2 }, results);
    }

    [Fact]
    public async Task SortBy_ShouldSupportThenByWithDirectionWithSqlite()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var query = database.Context.Products
            .SortBy(product => product.Name)
            .ThenBy(product => product.Id, SortDirection.Descending);

        var sql = query.ToQueryString();

        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DESC", sql, StringComparison.OrdinalIgnoreCase);

        var results = await query
            .Select(product => product.Id)
            .ToListAsync();

        Assert.Equal(new[] { 4, 1, 3, 2 }, results);
    }
}
