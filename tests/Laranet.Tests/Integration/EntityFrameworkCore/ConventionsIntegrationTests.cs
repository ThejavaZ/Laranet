using Laranet.Tests.Integration.Data.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.EntityFrameworkCore;

/// <summary>
/// Verifies the opt-in auditing and soft-delete conventions against SQLite.
/// </summary>
public class ConventionsIntegrationTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset UpdatedAt =
        new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChanges_ShouldSetCreatedAt_WhenAuditableEntityIsAdded()
    {
        var clock = new TestTimeProvider(CreatedAt);

        await using var database = await SqliteTestContextFactory.CreateAsync(
            timeProvider: clock);

        var record = new AuditedTestRecord { Name = "Alpha" };

        database.Context.AuditedRecords.Add(record);
        await database.Context.SaveChangesAsync();

        Assert.Equal(CreatedAt, record.CreatedAt);
        Assert.Null(record.UpdatedAt);
    }

    [Fact]
    public async Task SaveChanges_ShouldSetUpdatedAt_WhenAuditableEntityIsModified()
    {
        var clock = new TestTimeProvider(CreatedAt);

        await using var database = await SqliteTestContextFactory.CreateAsync(
            timeProvider: clock);

        var record = new AuditedTestRecord { Name = "Alpha" };

        database.Context.AuditedRecords.Add(record);
        await database.Context.SaveChangesAsync();

        clock.UtcNow = UpdatedAt;
        record.Name = "Beta";
        await database.Context.SaveChangesAsync();

        Assert.Equal(CreatedAt, record.CreatedAt);
        Assert.Equal(UpdatedAt, record.UpdatedAt);
    }

    [Fact]
    public async Task Remove_ShouldSoftDeleteEntity_WhenEntityIsSoftDeletable()
    {
        var clock = new TestTimeProvider(CreatedAt);

        await using var database = await SqliteTestContextFactory.CreateAsync(
            timeProvider: clock);

        var record = new SoftDeleteTestRecord { Name = "Alpha" };

        database.Context.SoftDeleteRecords.Add(record);
        await database.Context.SaveChangesAsync();

        clock.UtcNow = UpdatedAt;
        database.Context.SoftDeleteRecords.Remove(record);
        await database.Context.SaveChangesAsync();

        Assert.True(record.IsDeleted);
        Assert.Equal(UpdatedAt, record.DeletedAt);
        Assert.Equal(0, await database.Context.SoftDeleteRecords.CountAsync());
        Assert.Equal(
            1,
            await database.Context.SoftDeleteRecords
                .IgnoreQueryFilters()
                .CountAsync());
    }

    [Fact]
    public async Task SaveChanges_ShouldApplyConventions_WhenCalledSynchronously()
    {
        var clock = new TestTimeProvider(CreatedAt);

        await using var database = await SqliteTestContextFactory.CreateAsync(
            timeProvider: clock);

        var record = new AuditedSoftDeleteTestRecord { Name = "Alpha" };

        database.Context.AuditedSoftDeleteRecords.Add(record);
        database.Context.SaveChanges();

        clock.UtcNow = UpdatedAt;
        database.Context.AuditedSoftDeleteRecords.Remove(record);
        database.Context.SaveChanges();

        Assert.Equal(CreatedAt, record.CreatedAt);
        Assert.Equal(UpdatedAt, record.UpdatedAt);
        Assert.True(record.IsDeleted);
        Assert.Equal(UpdatedAt, record.DeletedAt);
        Assert.Equal(
            0,
            database.Context.AuditedSoftDeleteRecords.Count());
        Assert.Equal(
            1,
            database.Context.AuditedSoftDeleteRecords
                .IgnoreQueryFilters()
                .Count());
    }

    [Fact]
    public async Task Remove_ShouldSetUpdatedAt_WhenSoftDeletableEntityIsAuditable()
    {
        var clock = new TestTimeProvider(CreatedAt);

        await using var database = await SqliteTestContextFactory.CreateAsync(
            timeProvider: clock);

        var record = new AuditedSoftDeleteTestRecord { Name = "Alpha" };

        database.Context.AuditedSoftDeleteRecords.Add(record);
        await database.Context.SaveChangesAsync();

        Assert.Equal(CreatedAt, record.CreatedAt);
        Assert.Null(record.UpdatedAt);

        clock.UtcNow = UpdatedAt;
        database.Context.AuditedSoftDeleteRecords.Remove(record);
        await database.Context.SaveChangesAsync();

        Assert.True(record.IsDeleted);
        Assert.Equal(UpdatedAt, record.DeletedAt);
        Assert.Equal(UpdatedAt, record.UpdatedAt);
    }

    [Fact]
    public async Task Query_ShouldExcludeSoftDeletedEntities_ByDefault()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync();
        var context = database.Context;

        context.SoftDeleteRecords.AddRange(
            new SoftDeleteTestRecord { Name = "Active" },
            new SoftDeleteTestRecord { Name = "Deleted" });
        await context.SaveChangesAsync();

        var deleted = await context.SoftDeleteRecords
            .SingleAsync(record => record.Name == "Deleted");

        context.SoftDeleteRecords.Remove(deleted);
        await context.SaveChangesAsync();

        var names = await context.SoftDeleteRecords
            .Select(record => record.Name)
            .ToListAsync();

        Assert.Equal(["Active"], names);
    }

    [Fact]
    public async Task SoftDelete_ShouldBeReversible_WhenEntityIsRestored()
    {
        var clock = new TestTimeProvider(CreatedAt);

        await using var database = await SqliteTestContextFactory.CreateAsync(
            timeProvider: clock);

        var record = new SoftDeleteTestRecord { Name = "Alpha" };

        database.Context.SoftDeleteRecords.Add(record);
        await database.Context.SaveChangesAsync();

        database.Context.SoftDeleteRecords.Remove(record);
        await database.Context.SaveChangesAsync();

        record.IsDeleted = false;
        record.DeletedAt = null;
        await database.Context.SaveChangesAsync();

        Assert.Equal(
            1,
            await database.Context.SoftDeleteRecords.CountAsync());
    }

    [Fact]
    public async Task ApplySoftDeleteQueryFilters_ShouldComposeWithExistingFilter()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync();
        var context = database.Context;

        var visible = new FilteredSoftDeleteTestRecord { Name = "Visible" };
        var deleted = new FilteredSoftDeleteTestRecord { Name = "Deleted" };
        var hidden = new FilteredSoftDeleteTestRecord { Name = "Hidden" };
        var hiddenAndDeleted =
            new FilteredSoftDeleteTestRecord { Name = "Hidden" };

        context.FilteredSoftDeleteRecords.AddRange(
            visible,
            deleted,
            hidden,
            hiddenAndDeleted);
        await context.SaveChangesAsync();

        context.FilteredSoftDeleteRecords.RemoveRange(
            deleted,
            hiddenAndDeleted);
        await context.SaveChangesAsync();

        var visibleNames = await context.FilteredSoftDeleteRecords
            .Select(record => record.Name)
            .ToListAsync();

        Assert.Equal(["Visible"], visibleNames);

        Assert.Equal(
            4,
            await context.FilteredSoftDeleteRecords
                .IgnoreQueryFilters()
                .CountAsync());
    }

    [Fact]
    public async Task ApplySoftDeleteQueryFilters_ShouldComposeWithNamedFilter()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync();
        var context = database.Context;

        var visible =
            new NamedFilteredSoftDeleteTestRecord { Name = "Visible" };
        var deleted =
            new NamedFilteredSoftDeleteTestRecord { Name = "Deleted" };
        var hidden =
            new NamedFilteredSoftDeleteTestRecord { Name = "Hidden" };

        context.NamedFilteredSoftDeleteRecords.AddRange(
            visible,
            deleted,
            hidden);
        await context.SaveChangesAsync();

        context.NamedFilteredSoftDeleteRecords.Remove(deleted);
        await context.SaveChangesAsync();

        var names = await context.NamedFilteredSoftDeleteRecords
            .Select(record => record.Name)
            .ToListAsync();

        Assert.Equal(["Visible"], names);

        Assert.Equal(
            3,
            await context.NamedFilteredSoftDeleteRecords
                .IgnoreQueryFilters()
                .CountAsync());
    }

    [Fact]
    public async Task SaveChanges_ShouldNotAffectEntities_WithoutConventionInterfaces()
    {
        await using var database = await SqliteTestContextFactory.CreateAsync();
        var context = database.Context;

        context.Products.Add(new DataTestProduct { Name = "Keyboard" });
        await context.SaveChangesAsync();

        Assert.Equal(1, await context.Products.CountAsync());
        Assert.Equal(
            "Keyboard",
            await context.Products
                .Select(product => product.Name)
                .SingleAsync());
    }
}
