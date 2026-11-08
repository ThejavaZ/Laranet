using Laranet.Data.Extensions;
using Laranet.Data.Sorting;
using Laranet.Tests.Integration.Data.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Data;

public class DynamicSortIntegrationTests
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
    public async Task OrderBy_ShouldTranslateToSql()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var query = database.Context.Products.OrderBy("Name");

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
    public async Task OrderBy_ShouldTranslateDescendingToSql()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var query = database.Context.Products.OrderBy(
            "Id",
            SortDirection.Descending
        );

        var sql = query.ToQueryString();

        Assert.Contains("DESC", sql, StringComparison.OrdinalIgnoreCase);

        var results = await query
            .Select(product => product.Id)
            .ToListAsync();

        Assert.Equal(new[] { 4, 3, 2, 1 }, results);
    }

    [Fact]
    public async Task ThenBy_ShouldTranslateCompositeOrderingToSql()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var query = database.Context.Products
            .OrderBy("Name")
            .ThenBy("Id", SortDirection.Descending);

        var sql = query.ToQueryString();

        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DESC", sql, StringComparison.OrdinalIgnoreCase);

        var results = await query
            .Select(product => product.Id)
            .ToListAsync();

        Assert.Equal(new[] { 4, 1, 3, 2 }, results);
    }

    [Fact]
    public async Task OrderBy_ShouldSort_WhenPropertyIsAllowedByAllowlist()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var query = database.Context.Products.OrderBy("Name", ["Name"]);

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
    public async Task OrderBy_ShouldRejectPropertyNotAllowed_BeforeExecutingSql()
    {
        var interceptor = new CommandCaptureInterceptor();

        await using var database = await SqliteTestContextFactory.CreateAsync(
            interceptor,
            SeedProducts);

        Assert.Throws<ArgumentException>(() =>
        {
            var query = database.Context.Products.OrderBy("Id", ["Name"]);
            _ = query.ToList();
        });

        Assert.Empty(interceptor.Commands);
    }

    [Fact]
    public async Task OrderBy_ShouldRejectEveryProperty_WhenAllowlistIsEmpty()
    {
        var interceptor = new CommandCaptureInterceptor();

        await using var database = await SqliteTestContextFactory.CreateAsync(
            interceptor,
            SeedProducts);

        Assert.Throws<ArgumentException>(() =>
        {
            var query = database.Context.Products.OrderBy(
                "Name",
                Array.Empty<string>());
            _ = query.ToList();
        });

        Assert.Empty(interceptor.Commands);
    }

    [Fact]
    public async Task ThenBy_ShouldSort_WhenPropertyIsAllowedByAllowlist()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync(
            seed: SeedProducts);

        var query = database.Context.Products
            .OrderBy("Name")
            .ThenBy("Id", ["Id"], SortDirection.Descending);

        var sql = query.ToQueryString();

        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DESC", sql, StringComparison.OrdinalIgnoreCase);

        var results = await query
            .Select(product => product.Id)
            .ToListAsync();

        Assert.Equal(new[] { 4, 1, 3, 2 }, results);
    }
}
