using Laranet.Data.Extensions;
using Laranet.EntityFrameworkCore.Extensions;
using Laranet.Tests.Integration.Data.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Data;

/// <summary>
/// Verifies the documented query behavior against the provider selected by
/// <see cref="ProviderTestData"/>. SQLite runs by default; SQL Server and PostgreSQL
/// run when the CI service containers are available.
/// </summary>
public class ProviderCompatibilityTests
{
    private static void SeedSearchProducts(DataTestDbContext context)
    {
        context.Products.AddRange(
            new DataTestProduct { Name = "Mechanical Keyboard" },
            new DataTestProduct { Name = "Gaming Mouse" },
            new DataTestProduct { Name = "Wireless Keyboard" },
            new DataTestProduct { Name = null }
        );
    }

    private static void SeedMultiColumnProducts(DataTestDbContext context)
    {
        context.Products.AddRange(
            new DataTestProduct
            {
                Name = "Mechanical Keyboard",
                Description = "Wired",
            },
            new DataTestProduct
            {
                Name = "Gaming Mouse",
                Description = "Mechanical Keyboard switches",
            },
            new DataTestProduct
            {
                Name = "Monitor",
                Description = null,
            }
        );
    }

    private static void SeedSortProducts(DataTestDbContext context)
    {
        context.Products.AddRange(
            new DataTestProduct { Name = "Keyboard" },
            new DataTestProduct { Name = "Mouse" },
            new DataTestProduct { Name = "Monitor" },
            new DataTestProduct { Name = "Keyboard" }
        );
    }

    private static void SeedPagedProducts(DataTestDbContext context)
    {
        context.Products.AddRange(
            Enumerable.Range(1, 50)
                .Select(index => new DataTestProduct
                {
                    Name = $"Product {index:D2}",
                })
        );
    }

    [Theory]
    [MemberData(
        nameof(ProviderTestData.Providers),
        MemberType = typeof(ProviderTestData))]
    public async Task Search_ShouldReturnMatchingItems(
        DataTestProvider provider)
    {
        await using var database = await ProviderTestContextFactory.CreateAsync(
            provider,
            seed: SeedSearchProducts);

        var results = await database.Context.Products
            .Search(product => product.Name, "Keyboard")
            .OrderBy(product => product.Id)
            .ToListAsync();

        Assert.Equal(2, results.Count);

        Assert.All(results, product =>
            Assert.Contains("Keyboard", product.Name!)
        );
    }

    [Theory]
    [MemberData(
        nameof(ProviderTestData.Providers),
        MemberType = typeof(ProviderTestData))]
    public async Task Search_ShouldMatchAnyColumn(
        DataTestProvider provider)
    {
        await using var database = await ProviderTestContextFactory.CreateAsync(
            provider,
            seed: SeedMultiColumnProducts);

        var results = await database.Context.Products
            .Search(
                [product => product.Name, product => product.Description],
                "Keyboard")
            .OrderBy(product => product.Id)
            .ToListAsync();

        Assert.Equal(2, results.Count);
    }

    [Theory]
    [MemberData(
        nameof(ProviderTestData.Providers),
        MemberType = typeof(ProviderTestData))]
    public async Task Search_ShouldParameterizeTerm(
        DataTestProvider provider)
    {
        var interceptor = new CommandCaptureInterceptor();

        await using var database = await ProviderTestContextFactory.CreateAsync(
            provider,
            interceptor,
            SeedSearchProducts);

        var results = await database.Context.Products
            .Search(product => product.Name, "Keyboard")
            .OrderBy(product => product.Id)
            .ToListAsync();

        Assert.Equal(2, results.Count);
        Assert.NotEmpty(interceptor.Commands);

        Assert.Contains(
            interceptor.Commands,
            command => command.Parameters.Any(parameter =>
                parameter.Value is string value &&
                value.Contains("Keyboard", StringComparison.Ordinal))
        );

        Assert.All(interceptor.Commands, command =>
            Assert.DoesNotContain(
                "'Keyboard'",
                command.CommandText,
                StringComparison.Ordinal)
        );
    }

    [Theory]
    [MemberData(
        nameof(ProviderTestData.Providers),
        MemberType = typeof(ProviderTestData))]
    public async Task SortBy_ShouldOrderResults(
        DataTestProvider provider)
    {
        await using var database = await ProviderTestContextFactory.CreateAsync(
            provider,
            seed: SeedSortProducts);

        var ids = await database.Context.Products
            .SortBy(product => product.Name)
            .ThenByDescending(product => product.Id)
            .Select(product => product.Id)
            .ToListAsync();

        Assert.Equal(new[] { 4, 1, 3, 2 }, ids);
    }

    [Theory]
    [MemberData(
        nameof(ProviderTestData.Providers),
        MemberType = typeof(ProviderTestData))]
    public async Task OrderBy_ShouldSortWithAllowlist(
        DataTestProvider provider)
    {
        await using var database = await ProviderTestContextFactory.CreateAsync(
            provider,
            seed: SeedSortProducts);

        var names = await database.Context.Products
            .OrderBy("Name", ["Name"])
            .Select(product => product.Name)
            .ToListAsync();

        Assert.Equal(
            new[] { "Keyboard", "Keyboard", "Monitor", "Mouse" },
            names
        );
    }

    [Theory]
    [MemberData(
        nameof(ProviderTestData.Providers),
        MemberType = typeof(ProviderTestData))]
    public async Task WhereIf_ShouldApplyConditionalFilter(
        DataTestProvider provider)
    {
        await using var database = await ProviderTestContextFactory.CreateAsync(
            provider,
            seed: SeedSortProducts);

        var filtered = await database.Context.Products
            .WhereIf(true, product => product.Name == "Keyboard")
            .ToListAsync();

        Assert.Equal(2, filtered.Count);

        var unfiltered = await database.Context.Products
            .WhereIf(false, product => product.Name == "Keyboard")
            .ToListAsync();

        Assert.Equal(4, unfiltered.Count);
    }

    [Theory]
    [MemberData(
        nameof(ProviderTestData.Providers),
        MemberType = typeof(ProviderTestData))]
    public async Task Paginate_ShouldReturnRequestedPage(
        DataTestProvider provider)
    {
        await using var database = await ProviderTestContextFactory.CreateAsync(
            provider,
            seed: SeedPagedProducts);

        var result = database.Context.Products
            .OrderBy(product => product.Id)
            .Paginate(page: 2, pageSize: 10);

        Assert.Equal(50, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal("Product 11", result.Items[0].Name);
        Assert.Equal("Product 20", result.Items[^1].Name);
    }

    [Theory]
    [MemberData(
        nameof(ProviderTestData.Providers),
        MemberType = typeof(ProviderTestData))]
    public async Task PaginateAsync_ShouldReturnRequestedPage(
        DataTestProvider provider)
    {
        await using var database = await ProviderTestContextFactory.CreateAsync(
            provider,
            seed: SeedPagedProducts);

        var result = await database.Context.Products
            .OrderBy(product => product.Id)
            .PaginateAsync(page: 2, pageSize: 10);

        Assert.Equal(50, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal("Product 11", result.Items[0].Name);
        Assert.Equal("Product 20", result.Items[^1].Name);
    }

    [Theory]
    [MemberData(
        nameof(ProviderTestData.Providers),
        MemberType = typeof(ProviderTestData))]
    public async Task Paginate_ShouldComposeWithSearchAndSort(
        DataTestProvider provider)
    {
        await using var database = await ProviderTestContextFactory.CreateAsync(
            provider,
            seed: SeedPagedProducts);

        var result = database.Context.Products
            .Search(product => product.Name, "Product")
            .SortBy(product => product.Name)
            .Paginate(page: 2, pageSize: 10);

        Assert.Equal(50, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal("Product 11", result.Items[0].Name);
        Assert.Equal("Product 20", result.Items[^1].Name);
    }
}
