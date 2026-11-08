using Laranet.Data.Extensions;
using Laranet.Tests.Integration.Data.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Data;

public class PaginationIntegrationTests
{
    private static async Task<(
        SqliteConnection Connection,
        SearchTestDbContext Context)> CreateContextAsync(
        CommandCaptureInterceptor? interceptor = null)
    {
        var connection = new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var optionsBuilder = new DbContextOptionsBuilder<SearchTestDbContext>()
            .UseSqlite(connection);

        if (interceptor is not null)
        {
            optionsBuilder.AddInterceptors(interceptor);
        }

        var context = new SearchTestDbContext(optionsBuilder.Options);

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
    public async Task Paginate_ShouldExecuteCountAndPagedQueryWithOrdering()
    {
        var interceptor = new CommandCaptureInterceptor();

        var (connection, context) = await CreateContextAsync(interceptor);

        await using var dbConnection = connection;
        await using var dbContext = context;

        interceptor.Clear();

        var result = context.Products
            .OrderBy(product => product.Id)
            .Paginate(page: 2, pageSize: 10);

        Assert.Equal(50, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.Items.Count);

        Assert.Equal("Product 11", result.Items[0].Name);
        Assert.Equal("Product 20", result.Items[^1].Name);

        Assert.Equal(2, interceptor.Commands.Count);

        Assert.Contains(
            "COUNT",
            interceptor.Commands[0].CommandText,
            StringComparison.OrdinalIgnoreCase
        );

        var pageCommand = interceptor.Commands[1];

        Assert.Contains(
            "ORDER BY",
            pageCommand.CommandText,
            StringComparison.OrdinalIgnoreCase
        );

        Assert.Contains(
            "LIMIT",
            pageCommand.CommandText,
            StringComparison.OrdinalIgnoreCase
        );

        Assert.Contains(
            "OFFSET",
            pageCommand.CommandText,
            StringComparison.OrdinalIgnoreCase
        );
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
