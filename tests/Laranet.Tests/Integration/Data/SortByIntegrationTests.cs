
using Laranet.Data.Extensions;
using Laranet.Data.Sorting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Data;

public class SortByIntegrationTests
{
    private static async Task<(
        SqliteConnection Connection,
        SearchTestDbContext Context)> CreateContextAsync()
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
    public async Task SortBy_ShouldTranslateAscendingToSql()
    {
        var (connection, context) = await CreateContextAsync();

        await using var dbConnection = connection;
        await using var dbContext = context;

        var query = context.Products
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
        var (connection, context) = await CreateContextAsync();

        await using var dbConnection = connection;
        await using var dbContext = context;

        var query = context.Products
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
        var (connection, context) = await CreateContextAsync();

        await using var dbConnection = connection;
        await using var dbContext = context;

        var query = context.Products
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
}
