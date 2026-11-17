using Laranet.Admin;

namespace Laranet.Tests.Unit.Admin;

public class AdminResourceDiscoveryTests
{
    [Fact]
    public void Discover_ShouldFindTypedAndStaticAttributedResources()
    {
        var types = AdminResourceDiscovery.Discover(
            typeof(AdminResourceDiscoveryTests).Assembly);

        Assert.Contains(typeof(TypedDiscoveryResource), types);
        Assert.Contains(typeof(StaticDiscoveryResource), types);
        Assert.DoesNotContain(typeof(UnattributedResource), types);
    }

    [Fact]
    public void RegisterAll_ShouldAddDiscoveredDefinitionsToRegistry()
    {
        var registry = new AdminResourceRegistry();

        AdminResourceDiscovery.RegisterAll(
            registry,
            typeof(AdminResourceDiscoveryTests).Assembly);

        Assert.Contains(registry.Resources, resource => resource.Name == "TypedDiscovery");
        Assert.Contains(registry.Resources, resource => resource.Name == "StaticDiscovery");
    }

    [Fact]
    public void Discover_ShouldIgnoreAttributedTypesWithoutDefinition()
    {
        var types = AdminResourceDiscovery.Discover(
            typeof(AdminResourceDiscoveryTests).Assembly);

        Assert.DoesNotContain(typeof(AttributeOnlyResource), types);
    }

    private sealed class DiscoveryModel
    {
        public int Id { get; set; }
    }

    [AdminResource(Name = "TypedDiscovery")]
    private sealed class TypedDiscoveryResource : AdminResource<DiscoveryModel>
    {
        protected override void Configure(AdminResourceBuilder<DiscoveryModel> resource)
        {
            resource.Field(model => model.Id);
        }
    }

    [AdminResource(Name = "StaticDiscovery")]
    private static class StaticDiscoveryResource
    {
        public static AdminResource Definition { get; } = new(
            "StaticDiscovery",
            typeof(DiscoveryModel),
            [new AdminField("Id", "ID", typeof(int), IsKey: true)],
            [],
            []);
    }

    [AdminResource(Name = "AttributeOnly")]
    private sealed class AttributeOnlyResource;

    private sealed class UnattributedResource;
}
