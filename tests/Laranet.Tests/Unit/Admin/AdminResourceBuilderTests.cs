using Laranet.Admin;

namespace Laranet.Tests.Unit.Admin;

public class AdminResourceBuilderTests
{
    [Fact]
    public void TypedResource_ShouldBuildSearchFilterOrderFormAndPermissionMetadata()
    {
        var definition = new ProductResource().Definition;

        var name = Assert.Single(definition.Fields, field => field.Name == "Name");
        var price = Assert.Single(definition.Fields, field => field.Name == "Price");

        Assert.Equal("Products", definition.Name);
        Assert.Equal("Product", definition.SingularName);
        Assert.Equal("box", definition.IconName);
        Assert.True(name.IsSearchable);
        Assert.True(name.IsOrderable);
        Assert.Equal("General", name.Section);
        Assert.Equal("currency", price.Format);
        Assert.Contains(definition.Filters, filter => filter.Name == "IsActive");
        Assert.Equal("Name", definition.DefaultOrderField);
        Assert.False(definition.DefaultOrderDescending);
        Assert.Equal("Admin.Products", definition.PermissionPolicy);
        Assert.NotNull(definition.Form);
        Assert.Contains(definition.Form.Fields, field => field.Name == "Name");
    }

    [Fact]
    public void TypedResource_ShouldMarkIdAsKey()
    {
        var definition = new ProductResource().Definition;

        Assert.Equal("Id", definition.KeyField.Name);
        Assert.Equal(typeof(int), definition.KeyField.Type);
    }

    [Fact]
    public void AdminResource_ShouldDeriveSingularAndDefaultIcon_WhenNotConfigured()
    {
        var definition = new AdminResource(
            "Categories",
            typeof(Product),
            [new AdminField("Id", "ID", typeof(int), IsKey: true)],
            [],
            []);

        Assert.Equal("Category", definition.SingularName);
        Assert.Equal("box", definition.IconName);
    }

    [Fact]
    public void AdminResource_ShouldUseExplicitSingularAndIcon_WhenConfigured()
    {
        var definition = new AdminResource(
            "People",
            typeof(Product),
            [new AdminField("Id", "ID", typeof(int), IsKey: true)],
            [],
            [],
            Singular: "Person",
            Icon: "users");

        Assert.Equal("Person", definition.SingularName);
        Assert.Equal("users", definition.IconName);
    }

    private sealed class ProductResource : AdminResource<Product>
    {
        protected override void Configure(AdminResourceBuilder<Product> resource)
        {
            resource.Field(product => product.Id, "ID").Orderable();
            resource.Field(product => product.Name).Searchable().Orderable().Section("General");
            resource.Field(product => product.Price).FormatAs("currency");
            resource.Filter(product => product.IsActive, "Active");
            resource.Action("Delete", "Delete product", mutatesData: true);
            resource.Permission("Admin.Products");
            resource.OrderBy(product => product.Name);
        }

        protected override string? Singular => "Product";

        protected override string? Icon => "box";
    }

    private sealed class Product
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public bool IsActive { get; set; }
    }
}
