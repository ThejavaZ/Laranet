using Laranet.Admin;
using Laranet.Admin.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Admin;

public class EfCoreAdminDataAdapterTests
{
    [Fact]
    public async Task Adapter_ShouldSearchFilterOrderAndPaginateWithSqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDb(connection);
        db.Products.AddRange(
            new AdminProduct { Name = "Keyboard", Price = 99m, IsActive = true },
            new AdminProduct { Name = "Monitor", Price = 249m, IsActive = true },
            new AdminProduct { Name = "Mouse", Price = 39m, IsActive = false });
        await db.SaveChangesAsync();
        var adapter = new EfCoreAdminDataAdapter(db);

        var page = await adapter.ListAsync(
            Resource,
            new AdminQuery(
                Page: 1,
                PageSize: 1,
                Search: "o",
                Filters: new Dictionary<string, string?> { ["IsActive"] = "true" },
                SortBy: "Price",
                SortDescending: true));

        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("Monitor", page.Items[0]["Name"]);
    }

    [Fact]
    public async Task Adapter_ShouldCreateUpdateAndSoftDelete()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDb(connection);
        var adapter = new EfCoreAdminDataAdapter(db);

        var created = await adapter.CreateAsync(
            Resource,
            new Dictionary<string, object?>
            {
                ["Name"] = "Keyboard",
                ["Price"] = "99",
                ["IsActive"] = "true",
            });
        var id = Convert.ToInt32(created["Id"]);

        await adapter.UpdateAsync(
            Resource,
            id,
            new Dictionary<string, object?> { ["Name"] = "Mechanical Keyboard" });
        await adapter.DeleteAsync(Resource, id);

        var entity = await db.Products.SingleAsync();

        Assert.Equal("Mechanical Keyboard", entity.Name);
        Assert.True(entity.IsDeleted);
        Assert.NotNull(entity.DeletedAt);
    }

    [Fact]
    public async Task Adapter_ShouldApplyValuesFromFields_WhenFormIsNotDefined()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDb(connection);
        var adapter = new EfCoreAdminDataAdapter(db);

        await adapter.CreateAsync(
            ResourceWithoutForm,
            new Dictionary<string, object?>
            {
                ["Id"] = "",
                ["Name"] = "Keyboard",
                ["Price"] = "99",
                ["IsActive"] = "true",
            });

        var entity = await db.Products.SingleAsync();

        Assert.Equal("Keyboard", entity.Name);
        Assert.Equal(99m, entity.Price);
        Assert.True(entity.IsActive);
        Assert.False(entity.IsDeleted);
    }

    private static AdminDbContext CreateDb(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AdminDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new AdminDbContext(options);
        db.Database.EnsureCreated();

        return db;
    }

    private static AdminResource Resource { get; } = new(
        "Products",
        typeof(AdminProduct),
        [
            new AdminField("Id", "ID", typeof(int), IsKey: true, IsReadOnly: true),
            new AdminField("Name", "Name", typeof(string), IsSearchable: true, IsOrderable: true),
            new AdminField("Price", "Price", typeof(decimal), IsOrderable: true),
            new AdminField("IsActive", "Active", typeof(bool), IsFilterable: true),
            new AdminField("IsDeleted", "Deleted", typeof(bool), IsReadOnly: true),
            new AdminField("DeletedAt", "Deleted at", typeof(DateTimeOffset?), IsReadOnly: true),
        ],
        [new AdminFilter("IsActive", "Active", typeof(bool))],
        [],
        Form: new AdminForm(
        [
            new AdminField("Name", "Name", typeof(string)),
            new AdminField("Price", "Price", typeof(decimal)),
            new AdminField("IsActive", "Active", typeof(bool)),
        ]));

    private static AdminResource ResourceWithoutForm { get; } = new(
        "Products",
        typeof(AdminProduct),
        [
            new AdminField("Id", "ID", typeof(int), IsKey: true, IsReadOnly: true),
            new AdminField("Name", "Name", typeof(string)),
            new AdminField("Price", "Price", typeof(decimal)),
            new AdminField("IsActive", "Active", typeof(bool)),
            new AdminField("IsDeleted", "Deleted", typeof(bool), IsReadOnly: true),
            new AdminField("DeletedAt", "Deleted at", typeof(DateTimeOffset?), IsReadOnly: true),
        ],
        [],
        []);

    private sealed class AdminProduct
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }
    }

    private sealed class AdminDbContext(DbContextOptions<AdminDbContext> options)
        : DbContext(options)
    {
        public DbSet<AdminProduct> Products => Set<AdminProduct>();
    }
}
