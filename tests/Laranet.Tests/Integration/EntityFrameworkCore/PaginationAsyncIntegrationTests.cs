using Laranet.Data.Extensions;
using Laranet.EntityFrameworkCore.Extensions;
using Laranet.Tests.Integration.Data.TestSupport;

namespace Laranet.Tests.Integration.EntityFrameworkCore;

public class PaginationAsyncIntegrationTests
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
    public async Task PaginateAsync_ShouldExecuteCountAndPagedQueryWithOrdering()
    {
        var interceptor = new CommandCaptureInterceptor();

        await using var database = await SqliteTestContextFactory.CreateAsync(
            interceptor,
            SeedProducts);

        var result = await database.Context.Products
            .OrderBy(product => product.Id)
            .PaginateAsync(page: 2, pageSize: 10);

        Assert.Equal(50, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(5, result.TotalPages);
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
    public async Task PaginateAsync_ShouldUseDefaultValues()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var result = await database.Context.Products
            .OrderBy(product => product.Id)
            .PaginateAsync();

        Assert.Equal(1, result.Page);
        Assert.Equal(15, result.PageSize);
        Assert.Equal(50, result.TotalCount);
        Assert.Equal(4, result.TotalPages);
        Assert.Equal(15, result.Items.Count);
    }

    [Fact]
    public async Task PaginateAsync_ShouldRespectMaximumPageSize()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var result = await database.Context.Products
            .OrderBy(product => product.Id)
            .PaginateAsync(pageSize: 200, maxPageSize: 25);

        Assert.Equal(25, result.PageSize);
        Assert.Equal(25, result.Items.Count);
        Assert.Equal(50, result.TotalCount);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(10)]
    public async Task PaginateAsync_ShouldExecuteOnlyCount_WhenPageIsOutOfRange(
        int page)
    {
        var interceptor = new CommandCaptureInterceptor();

        await using var database = await SqliteTestContextFactory.CreateAsync(
            interceptor,
            SeedProducts);

        var result = await database.Context.Products
            .OrderBy(product => product.Id)
            .PaginateAsync(page: page, pageSize: 10);

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
    public async Task PaginateAsync_ShouldExecuteOnlyCount_WhenCollectionIsEmpty()
    {
        var interceptor = new CommandCaptureInterceptor();

        await using var database = await SqliteTestContextFactory.CreateAsync(
            interceptor);

        var result = await database.Context.Products
            .OrderBy(product => product.Id)
            .PaginateAsync(page: 2, pageSize: 10);

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

    [Fact]
    public async Task PaginateAsync_ShouldComposeWithSearchAndSort()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var result = await database.Context.Products
            .Search(product => product.Name, "Product")
            .SortBy(product => product.Name)
            .PaginateAsync(page: 2, pageSize: 10);

        Assert.Equal(50, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal("Product 11", result.Items[0].Name);
        Assert.Equal("Product 20", result.Items[^1].Name);
    }

    [Fact]
    public async Task PaginateAsync_ShouldHonorCancellationToken()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        using var cancellation = new CancellationTokenSource();

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            database.Context.Products
                .OrderBy(product => product.Id)
                .PaginateAsync(
                    page: 1,
                    pageSize: 10,
                    cancellationToken: cancellation.Token)
        );
    }
}
