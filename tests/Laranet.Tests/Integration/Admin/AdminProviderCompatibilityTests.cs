using Laranet.Admin;
using Laranet.Admin.EntityFrameworkCore;
using Laranet.Tests.Integration.Data.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Laranet.Tests.Integration.Admin;

public class AdminProviderCompatibilityTests
{
    private static AdminResource Resource { get; } = new(
        "Products",
        typeof(AdminProviderContextFactory.AdminProviderProduct),
        [
            new AdminField("Id", "ID", typeof(Guid), IsKey: true, IsReadOnly: true),
            new AdminField("Name", "Name", typeof(string), IsSearchable: true, IsOrderable: true),
            new AdminField("IsActive", "Active", typeof(bool), IsFilterable: true),
            new AdminField("IsDeleted", "Deleted", typeof(bool), IsReadOnly: true),
            new AdminField("DeletedAt", "Deleted at", typeof(DateTimeOffset?), IsReadOnly: true),
        ],
        [new AdminFilter("IsActive", "Active", typeof(bool))],
        [],
        Form: new AdminForm(
        [
            new AdminField("Name", "Name", typeof(string)),
            new AdminField("IsActive", "Active", typeof(bool)),
        ]));

    private static Task<AdminProviderDatabase> CreateAsync(
        DataTestProvider provider)
    {
        return AdminProviderContextFactory.CreateAsync(provider, Seed);
    }

    private static void Seed(
        AdminProviderContextFactory.AdminProviderContext context)
    {
        context.Products.AddRange(
            new AdminProviderContextFactory.AdminProviderProduct
            {
                Name = "Keyboard",
                IsActive = true,
            },
            new AdminProviderContextFactory.AdminProviderProduct
            {
                Name = "Monitor",
                IsActive = true,
            },
            new AdminProviderContextFactory.AdminProviderProduct
            {
                Name = "Mouse",
                IsActive = false,
            });
    }

    [Theory]
    [MemberData(
        nameof(ProviderTestData.Providers),
        MemberType = typeof(ProviderTestData))]
    public async Task List_ShouldSearchFilterOrderAndPaginate(
        DataTestProvider provider)
    {
        await using var database = await CreateAsync(provider);
        var adapter = new EfCoreAdminDataAdapter(database.Context);

        var page = await adapter.ListAsync(
            Resource,
            new AdminQuery(
                Page: 1,
                PageSize: 1,
                Search: "o",
                Filters: new Dictionary<string, string?> { ["IsActive"] = "true" },
                SortBy: "Name",
                SortDescending: true));

        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("Monitor", page.Items[0]["Name"]);
    }

    [Theory]
    [MemberData(
        nameof(ProviderTestData.Providers),
        MemberType = typeof(ProviderTestData))]
    public async Task Crud_ShouldRoundTripWithSoftDelete(
        DataTestProvider provider)
    {
        await using var database = await CreateAsync(provider);
        var adapter = new EfCoreAdminDataAdapter(database.Context);

        var created = await adapter.CreateAsync(
            Resource,
            new Dictionary<string, object?>
            {
                ["Name"] = "Keyboard",
                ["IsActive"] = "true",
            });
        var id = (Guid)created["Id"]!;

        var updated = await adapter.UpdateAsync(
            Resource,
            id,
            new Dictionary<string, object?> { ["Name"] = "Mechanical Keyboard" });
        await adapter.DeleteAsync(Resource, id);
        var read = await adapter.ReadAsync(Resource, id);

        Assert.Equal("Mechanical Keyboard", updated["Name"]);
        Assert.NotNull(read);
        Assert.True(Convert.ToBoolean(read["IsDeleted"]));
        Assert.NotNull(read["DeletedAt"]);
    }
}
