using Laranet.Admin;

namespace Laranet.Tests.Unit.Admin;

public class AdminResourceValidationTests
{
    [Fact]
    public void Resource_Validate_ShouldRejectUnknownFilter()
    {
        var resource = new AdminResource(
            "Products",
            typeof(Product),
            [new AdminField("Id", "ID", typeof(int), IsKey: true)],
            [new AdminFilter("Unknown", "Unknown", typeof(string))],
            []);

        var exception = Assert.Throws<ArgumentException>(() => resource.Validate());

        Assert.Contains("unknown field", exception.Message);
    }

    [Fact]
    public void Resource_Validate_ShouldRejectUnknownRelation()
    {
        var resource = new AdminResource(
            "Products",
            typeof(Product),
            [new AdminField("Id", "ID", typeof(int), IsKey: true)],
            [],
            [],
            Relations: [new AdminRelation("Unknown", "Unknown", typeof(Product))]);

        var exception = Assert.Throws<ArgumentException>(() => resource.Validate());

        Assert.Contains("unknown field", exception.Message);
    }

    [Fact]
    public void Resource_Validate_ShouldRejectMultipleKeys()
    {
        var resource = new AdminResource(
            "Products",
            typeof(Product),
            [
                new AdminField("Id", "ID", typeof(int), IsKey: true),
                new AdminField("AltId", "Alt ID", typeof(int), IsKey: true),
            ],
            [],
            []);

        var exception = Assert.Throws<ArgumentException>(() => resource.Validate());

        Assert.Contains("exactly one key field", exception.Message);
    }

    [Fact]
    public void Registry_Add_ShouldValidateResource()
    {
        var registry = new AdminResourceRegistry();
        var resource = new AdminResource(
            "Products",
            typeof(Product),
            [new AdminField("Id", "ID", typeof(int), IsKey: true)],
            [new AdminFilter("Unknown", "Unknown", typeof(string))],
            []);

        var exception = Assert.Throws<ArgumentException>(() => registry.Add(resource));

        Assert.Contains("unknown field", exception.Message);
    }

    private sealed class Product
    {
        public int Id { get; set; }
    }
}
