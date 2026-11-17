using Laranet.Samples.Admin.Data;
using Laranet.Samples.Admin.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Admin;

public class ProductAdminAdapterTests
{
    [Fact]
    public async Task Adapter_ShouldListSearchAndPaginateProducts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDb(connection);
        db.Products.AddRange(
            new Product { Name = "Keyboard", Price = 99m },
            new Product { Name = "Monitor", Price = 249m },
            new Product { Name = "Mouse", Price = 39m });
        await db.SaveChangesAsync();
        var adapter = new ProductAdminAdapter(db);

        var page = await adapter.ListAsync(
            new ProductAdminResource().Definition,
            new Laranet.Admin.AdminQuery(Page: 1, PageSize: 1, Search: "Monitor"));

        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("Monitor", page.Items[0]["Name"]);
    }

    [Fact]
    public async Task Adapter_ShouldCreateUpdateAndDeleteProduct()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDb(connection);
        var adapter = new ProductAdminAdapter(db);

        var created = await adapter.CreateAsync(
            new ProductAdminResource().Definition,
            new Dictionary<string, object?>
            {
                ["Name"] = "Keyboard",
                ["Price"] = 99m,
                ["IsActive"] = true,
            });
        var id = Convert.ToInt32(created["Id"]);

        var updated = await adapter.UpdateAsync(
            new ProductAdminResource().Definition,
            id,
            new Dictionary<string, object?> { ["Name"] = "Mechanical Keyboard" });
        await adapter.DeleteAsync(new ProductAdminResource().Definition, id);

        Assert.Equal("Mechanical Keyboard", updated["Name"]);
        Assert.Null(await adapter.ReadAsync(new ProductAdminResource().Definition, id));
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
}
