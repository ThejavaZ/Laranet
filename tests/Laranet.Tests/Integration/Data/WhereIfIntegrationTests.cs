using Laranet.Data.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Data;

public class WhereIfIntegrationTests
{
    private static async Task<(SqliteConnection Connection, SearchTestDbContext Context)>
        CreateContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SearchTestDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new SearchTestDbContext(options);

        await context.Database.EnsureCreatedAsync();

        context.Products.AddRange(
            new SearchTestProduct { Name = "Keyboard" },
            new SearchTestProduct { Name = "Mouse" },
            new SearchTestProduct { Name = "Monitor" },
            new SearchTestProduct { Name = "Keyboard" }
        );

        await context.SaveChangesAsync();

        return (connection, context);
    }

    [Fact]
    public async Task WhereIf_ShouldTranslateToSql_WhenConditionIsTrue()
    {
        var (connection, context) = await CreateContextAsync();
        await using (connection)
        await using (context)
        {
            var query = context.Products
                .WhereIf(true, product => product.Name == "Keyboard");

            var sql = query.ToQueryString();
            var result = await query.ToListAsync();

            Assert.Contains("WHERE", sql);
            Assert.Equal(2, result.Count);
            Assert.All(result, product =>
            Assert.Equal("Keyboard", product.Name));
        }
    }

    [Fact]
    public async Task WhereIf_ShouldNotAddWhereClause_WhenConditionIsFalse()
    {
        var (connection, context) = await CreateContextAsync();
        await using (connection)
        await using (context)
        {
            var query = context.Products
                .WhereIf(false, product => product.Name == "Keyboard");

            var sql = query.ToQueryString();
            var result = await query.ToListAsync();

            Assert.DoesNotContain("WHERE", sql);
            Assert.Equal(4, result.Count);
        }
    }

    [Fact]
    public async Task WhereIf_ShouldComposeWithSearchAndDynamicSorting()
    {
        var (connection, context) = await CreateContextAsync();

        await using (connection)
        await using (context)
        {
            var searchQuery = context.Products
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

            Assert.Contains("WHERE", sql);
            Assert.Contains("ORDER BY", sql);
            Assert.Single(result);
            Assert.Equal(4, result[0].Id);
        }
    }
}
