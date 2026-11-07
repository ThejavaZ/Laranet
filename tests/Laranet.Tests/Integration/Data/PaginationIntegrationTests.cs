using Laranet.Data.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Data;

public class PaginationIntegrationTests
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
            Enumerable.Range(1, 50)
                .Select(index => new SearchTestProduct
                {
                    Name = $"Product {index:D2}"
                })
        );

        await context.SaveChangesAsync();

        return (connection, context);
    }

    [Fact]
    public async Task Paginate_ShouldTranslateToSql()
    {
        var (connection, context) = await CreateContextAsync();

        await using var dbConnection = connection;
        await using var dbContext = context;

        var query = context.Products
            .OrderBy(product => product.Id)
            .Skip(10)
            .Take(10);

        var sql = query.ToQueryString();

        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OFFSET", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Paginate_ShouldReturnCorrectPage()
    {
        var (connection, context) = await CreateContextAsync();

        await using var dbConnection = connection;
        await using var dbContext = context;

        var result = context.Products
            .OrderBy(product => product.Id)
            .Paginate(page: 2, pageSize: 10);

        Assert.Equal(50, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.Items.Count);

        Assert.Equal("Product 11", result.Items[0].Name);
        Assert.Equal("Product 20", result.Items[^1].Name);
    }

    [Fact]
    public async Task Paginate_ShouldCalculateTotalCount()
    {
        var (connection, context) = await CreateContextAsync();

        await using var dbConnection = connection;
        await using var dbContext = context;

        var result = context.Products
            .Where(product => product.Id <= 25)
            .OrderBy(product => product.Id)
            .Paginate(pageSize: 10);

        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(10, result.Items.Count);
    }

    [Fact]
    public async Task Paginate_ShouldRespectMaximumPageSize()
    {
        var (connection, context) = await CreateContextAsync();

        await using var dbConnection = connection;
        await using var dbContext = context;

        var result = context.Products
            .OrderBy(product => product.Id)
            .Paginate(pageSize: 200, maxPageSize: 25);

        Assert.Equal(25, result.PageSize);
        Assert.Equal(25, result.Items.Count);
        Assert.Equal(50, result.TotalCount);
    }

    [Fact]
    public async Task Paginate_ShouldComposeWithSearchAndSort()
    {
        var (connection, context) = await CreateContextAsync();

        await using var dbConnection = connection;
        await using var dbContext = context;

        var result = context.Products
            .Search(product => product.Name, "Product")
            .SortBy(product => product.Name)
            .Paginate(page: 2, pageSize: 10);

        Assert.Equal(50, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal("Product 11", result.Items[0].Name);
        Assert.Equal("Product 20", result.Items[^1].Name);
    }
}
