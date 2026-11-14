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

    private static readonly DateTimeOffset ConventionCreatedAt =
        new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset ConventionUpdatedAt =
        new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

    private static void SeedConventionRecords(DataTestDbContext context)
    {
        context.AuditedSoftDeleteRecords.AddRange(
            new AuditedSoftDeleteTestRecord { Name = "Alpha" },
            new AuditedSoftDeleteTestRecord { Name = "Beta" });
    }

    private static void SeedFilteredRecords(DataTestDbContext context)
    {
        context.FilteredSoftDeleteRecords.AddRange(
            new FilteredSoftDeleteTestRecord { Name = "Visible" },
            new FilteredSoftDeleteTestRecord { Name = "Deleted" },
            new FilteredSoftDeleteTestRecord { Name = "Hidden" });
    }

    [Theory]
    [MemberData(
        nameof(ProviderTestData.Providers),
        MemberType = typeof(ProviderTestData))]
    public async Task Conventions_ShouldPersistAuditingTimestamps(
        DataTestProvider provider)
    {
        var clock = new TestTimeProvider(ConventionCreatedAt);

        await using var database = await ProviderTestContextFactory.CreateAsync(
            provider,
            seed: SeedConventionRecords,
            timeProvider: clock);

        var context = database.Context;

        var records = await context.AuditedSoftDeleteRecords
            .OrderBy(record => record.Id)
            .ToListAsync();

        Assert.All(records, record =>
        {
            Assert.Equal(ConventionCreatedAt, record.CreatedAt);
            Assert.Null(record.UpdatedAt);
        });

        clock.UtcNow = ConventionUpdatedAt;
        records[0].Name = "Alpha Updated";
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var reloaded = await context.AuditedSoftDeleteRecords
            .SingleAsync(record => record.Id == records[0].Id);

        Assert.Equal(ConventionCreatedAt, reloaded.CreatedAt);
        Assert.Equal(ConventionUpdatedAt, reloaded.UpdatedAt);
    }

    [Theory]
    [MemberData(
        nameof(ProviderTestData.Providers),
        MemberType = typeof(ProviderTestData))]
    public async Task Conventions_ShouldFollowProviderDateTimeOffsetRules(
        DataTestProvider provider)
    {
        var offset = TimeSpan.FromHours(2);
        var clock = new TestTimeProvider(
            new DateTimeOffset(2026, 11, 12, 9, 0, 0, offset));

        await using var database = await ProviderTestContextFactory.CreateAsync(
            provider,
            timeProvider: clock);

        var context = database.Context;
        var record = new AuditedSoftDeleteTestRecord { Name = "Offset" };

        context.AuditedSoftDeleteRecords.Add(record);

        if (provider == DataTestProvider.PostgreSql)
        {
            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync());
            return;
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var reloaded = await context.AuditedSoftDeleteRecords
            .SingleAsync(entity => entity.Id == record.Id);

        Assert.Equal(clock.UtcNow, reloaded.CreatedAt);

        if (provider is DataTestProvider.Sqlite or DataTestProvider.SqlServer)
        {
            Assert.Equal(offset, reloaded.CreatedAt.Offset);
        }
        else
        {
            Assert.Equal(TimeSpan.Zero, reloaded.CreatedAt.Offset);
        }
    }

    [Theory]
    [MemberData(
        nameof(ProviderTestData.Providers),
        MemberType = typeof(ProviderTestData))]
    public async Task Conventions_ShouldHideSoftDeletedRecords(
        DataTestProvider provider)
    {
        var clock = new TestTimeProvider(ConventionCreatedAt);

        await using var database = await ProviderTestContextFactory.CreateAsync(
            provider,
            seed: SeedConventionRecords,
            timeProvider: clock);

        var context = database.Context;

        var record = await context.AuditedSoftDeleteRecords
            .OrderBy(entity => entity.Id)
            .FirstAsync();

        clock.UtcNow = ConventionUpdatedAt;
        context.AuditedSoftDeleteRecords.Remove(record);
        await context.SaveChangesAsync();

        Assert.True(record.IsDeleted);
        Assert.Equal(ConventionUpdatedAt, record.DeletedAt);
        Assert.Equal(ConventionUpdatedAt, record.UpdatedAt);
        Assert.Equal(
            1,
            await context.AuditedSoftDeleteRecords.CountAsync());
        Assert.Equal(
            2,
            await context.AuditedSoftDeleteRecords
                .IgnoreQueryFilters()
                .CountAsync());

        context.ChangeTracker.Clear();

        var reloaded = await context.AuditedSoftDeleteRecords
            .IgnoreQueryFilters()
            .SingleAsync(entity => entity.Id == record.Id);

        Assert.True(reloaded.IsDeleted);
        Assert.Equal(ConventionUpdatedAt, reloaded.DeletedAt);
        Assert.Equal(ConventionUpdatedAt, reloaded.UpdatedAt);
    }

    [Theory]
    [MemberData(
        nameof(ProviderTestData.Providers),
        MemberType = typeof(ProviderTestData))]
    public async Task Conventions_ShouldComposeWithExistingQueryFilter(
        DataTestProvider provider)
    {
        await using var database = await ProviderTestContextFactory.CreateAsync(
            provider,
            seed: SeedFilteredRecords);

        var context = database.Context;

        var deleted = await context.FilteredSoftDeleteRecords
            .SingleAsync(record => record.Name == "Deleted");

        context.FilteredSoftDeleteRecords.Remove(deleted);
        await context.SaveChangesAsync();

        var names = await context.FilteredSoftDeleteRecords
            .Select(record => record.Name)
            .ToListAsync();

        Assert.Equal(["Visible"], names);

        Assert.Equal(
            3,
            await context.FilteredSoftDeleteRecords
                .IgnoreQueryFilters()
                .CountAsync());
    }
}
