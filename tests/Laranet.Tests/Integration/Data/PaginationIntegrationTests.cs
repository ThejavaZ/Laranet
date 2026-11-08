using Laranet.Data.Extensions;
using Laranet.Tests.Integration.Data.TestSupport;

namespace Laranet.Tests.Integration.Data;

public class PaginationIntegrationTests
{
    private static void SeedProducts(DataTestDbContext context)
    {
        context.Products.AddRange(
            Enumerable.Range(1, 50)
                .Select(index => new DataTestProduct
                {
                    Name = $"Product {index:D2}"
                })
        );
    }

    [Fact]
    public async Task Paginate_ShouldExecuteCountAndPagedQueryWithOrdering()
    {
        var interceptor = new CommandCaptureInterceptor();

        await using var database = await SqliteTestContextFactory.CreateAsync(
            interceptor,
            SeedProducts);

        var result = database.Context.Products
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
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var result = database.Context.Products
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
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var result = database.Context.Products
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
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var result = database.Context.Products
            .OrderBy(product => product.Id)
            .Paginate(pageSize: 200, maxPageSize: 25);

        Assert.Equal(25, result.PageSize);
        Assert.Equal(25, result.Items.Count);
        Assert.Equal(50, result.TotalCount);
    }

    [Fact]
    public async Task Paginate_ShouldComposeWithSearchAndSort()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var result = database.Context.Products
            .Search(product => product.Name, "Product")
            .SortBy(product => product.Name)
            .Paginate(page: 2, pageSize: 10);

        Assert.Equal(50, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal("Product 11", result.Items[0].Name);
        Assert.Equal("Product 20", result.Items[^1].Name);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(10)]
    public async Task Paginate_ShouldExecuteOnlyCount_WhenPageIsOutOfRange(
        int page)
    {
        var interceptor = new CommandCaptureInterceptor();

        await using var database = await SqliteTestContextFactory.CreateAsync(
            interceptor,
            SeedProducts);

        var result = database.Context.Products
            .OrderBy(product => product.Id)
            .Paginate(page: page, pageSize: 10);

        Assert.Empty(result.Items);
        Assert.Equal(page, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(50, result.TotalCount);
        Assert.Equal(5, result.TotalPages);

        var command = Assert.Single(interceptor.Commands);

        Assert.Contains(
            "COUNT",
            command.CommandText,
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    public async Task Paginate_ShouldExecuteOnlyCount_WhenCollectionIsEmpty()
    {
        var interceptor = new CommandCaptureInterceptor();

        await using var database = await SqliteTestContextFactory.CreateAsync(
            interceptor);

        var result = database.Context.Products
            .OrderBy(product => product.Id)
            .Paginate(page: 2, pageSize: 10);

        Assert.Empty(result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);

        var command = Assert.Single(interceptor.Commands);

        Assert.Contains(
            "COUNT",
            command.CommandText,
            StringComparison.OrdinalIgnoreCase
        );
    }
}
