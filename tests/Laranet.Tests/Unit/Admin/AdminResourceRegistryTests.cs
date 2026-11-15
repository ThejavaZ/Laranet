using Laranet.Admin;

namespace Laranet.Tests.Unit.Admin;

public class AdminResourceRegistryTests
{
    [Fact]
    public void Add_ShouldRegisterResourceAndResolveItByName()
    {
        var registry = new AdminResourceRegistry();
        var resource = CreateResource();

        registry.Add(resource);

        Assert.Same(resource, registry.Get("products"));
        Assert.Single(registry.Resources);
        Assert.Equal("Id", registry.Get("Products").KeyField.Name);
    }

    [Fact]
    public void Add_ShouldRejectDuplicateNames()
    {
        var registry = new AdminResourceRegistry();
        registry.Add(CreateResource());

        var exception = Assert.Throws<InvalidOperationException>(
            () => registry.Add(CreateResource()));

        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void Add_ShouldRequireExactlyOneKey()
    {
        var registry = new AdminResourceRegistry();
        var fields = new[]
        {
            new AdminField("Name", "Name", typeof(string)),
        };
        var resource = new AdminResource(
            "Products",
            typeof(Product),
            fields,
            [],
            []);

        var exception = Assert.Throws<ArgumentException>(() => registry.Add(resource));

        Assert.Contains("one key field", exception.Message);
    }

    [Fact]
    public void Query_Normalize_ShouldClampPagingAndInitializeFilters()
    {
        var query = new AdminQuery(Page: 0, PageSize: 500).Normalize();

        Assert.Equal(1, query.Page);
        Assert.Equal(100, query.PageSize);
        Assert.NotNull(query.Filters);
    }

    private static AdminResource CreateResource()
    {
        return new AdminResource(
            "Products",
            typeof(Product),
            [
                new AdminField("Id", "ID", typeof(int), IsKey: true),
                new AdminField("Name", "Name", typeof(string)),
            ],
            [new AdminFilter("Name", "Name", typeof(string))],
            [
                new AdminAction("Delete", "Delete", MutatesData: true),
            ],
            "Admin.Products");
    }

    private sealed class Product
    {
    }
}
