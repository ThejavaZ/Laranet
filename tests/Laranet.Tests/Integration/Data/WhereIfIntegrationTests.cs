using Laranet.Data.Extensions;
using Laranet.Tests.Integration.Data.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Data;

public class WhereIfIntegrationTests
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
    public async Task WhereIf_ShouldTranslateToSql_WhenConditionIsTrue()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var query = database.Context.Products
            .WhereIf(true, product => product.Name == "Keyboard");

        var sql = query.ToQueryString();
        var result = await query.ToListAsync();

        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, result.Count);
        Assert.All(result, product =>
            Assert.Equal("Keyboard", product.Name));
    }

    [Fact]
    public async Task WhereIf_ShouldNotAddWhereClause_WhenConditionIsFalse()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var query = database.Context.Products
            .WhereIf(false, product => product.Name == "Keyboard");

        var sql = query.ToQueryString();
        var result = await query.ToListAsync();

        Assert.DoesNotContain("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task WhereIf_ShouldComposeWithSearchAndDynamicSorting()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var searchQuery = database.Context.Products
            .Search(product => product.Name, "Keyboard");

        var searchResults = await searchQuery.ToListAsync();

        Assert.Equal(2, searchResults.Count);

        var filteredQuery = searchQuery
            .WhereIf(true, product => product.Id > 1);

        var filteredResults = await filteredQuery.ToListAsync();

        Assert.Single(filteredResults);
        Assert.Equal(4, filteredResults[0].Id);

        var sortedQuery = filteredQuery
            .OrderBy("Id", Laranet.Data.Sorting.SortDirection.Descending);

        var sql = sortedQuery.ToQueryString();
        var result = await sortedQuery.ToListAsync();

        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Single(result);
        Assert.Equal(4, result[0].Id);
    }
}
