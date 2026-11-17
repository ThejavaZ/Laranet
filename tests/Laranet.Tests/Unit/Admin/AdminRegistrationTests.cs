using Laranet.Admin;
using Laranet.Admin.AspNetCore;
using Laranet.Admin.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Laranet.Tests.Unit.Admin;

public class AdminRegistrationTests
{
    [Fact]
    public void AddLaranetAdmin_WithExplicitResource_ShouldPopulateRegistry()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLaranetAdmin(options => options.AddResource(ProductsDefinition));

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IAdminResourceRegistry>();

        Assert.Contains(registry.Resources, resource => resource.Name == "Products");
    }

    [Fact]
    public void AddLaranetAdmin_WithTypedResource_ShouldBuildDefinition()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLaranetAdmin(options => options.AddResource<TypedRegistrationResource>());

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IAdminResourceRegistry>();
        var resource = Assert.Single(
            registry.Resources,
            item => item.Name == "TypedProducts");

        Assert.Contains(resource.Fields, field => field.IsKey);
        Assert.Equal("Admin.TypedProducts", resource.PermissionPolicy);
    }

    [Fact]
    public void AddLaranetAdmin_WithAdapterFactory_ShouldRegisterAdapter()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLaranetAdmin(options => options.AddAdapter(_ => new EfCoreAdminDataAdapter(
            new RegistrationDbContext(
                new DbContextOptionsBuilder<RegistrationDbContext>()
                    .UseSqlite("Data Source=:memory:")
                    .Options))));

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IAdminDataAdapter)
            && descriptor.ImplementationFactory is not null);
    }

    [Fact]
    public async Task AddLaranetAdmin_ShouldRegisterBasePolicyWhenAbsent()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLaranetAdmin(options => options.AddResource(ProductsDefinition));

        using var provider = services.BuildServiceProvider();
        var policy = await provider
            .GetRequiredService<IAuthorizationPolicyProvider>()
            .GetPolicyAsync("Admin.Products");

        Assert.NotNull(policy);
        Assert.NotEmpty(policy.Requirements);
    }

    [Fact]
    public async Task AddLaranetAdmin_ExplicitPolicyBeforeRegistration_ShouldWin()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationBuilder()
            .AddPolicy("Admin.Products", policy =>
                policy.AddRequirements(new TestRequirement()));
        services.AddLaranetAdmin(options => options.AddResource(ProductsDefinition));

        using var provider = services.BuildServiceProvider();
        var policy = await provider
            .GetRequiredService<IAuthorizationPolicyProvider>()
            .GetPolicyAsync("Admin.Products");

        Assert.Contains(policy!.Requirements, requirement => requirement is TestRequirement);
    }

    [Fact]
    public async Task AddLaranetAdmin_ExplicitPolicyAfterRegistration_ShouldWin()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLaranetAdmin(options => options.AddResource(ProductsDefinition));
        services.AddAuthorizationBuilder()
            .AddPolicy("Admin.Products", policy =>
                policy.AddRequirements(new TestRequirement()));

        using var provider = services.BuildServiceProvider();
        var policy = await provider
            .GetRequiredService<IAuthorizationPolicyProvider>()
            .GetPolicyAsync("Admin.Products");

        Assert.Contains(policy!.Requirements, requirement => requirement is TestRequirement);
    }

    [Fact]
    public void UseEfCore_ShouldRegisterEfCoreDataAdapter()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<RegistrationDbContext>(options =>
            options.UseSqlite("Data Source=:memory:"));
        services.AddLaranetAdmin(options => options.UseEfCore<RegistrationDbContext>());

        using var provider = services.BuildServiceProvider();

        Assert.IsType<EfCoreAdminDataAdapter>(
            provider.GetRequiredService<IAdminDataAdapter>());
    }

    private static AdminResource ProductsDefinition { get; } = new(
        "Products",
        typeof(RegistrationModel),
        [new AdminField("Id", "ID", typeof(int), IsKey: true)],
        [],
        [],
        "Admin.Products");

    private sealed class RegistrationModel
    {
        public int Id { get; set; }
    }

    [AdminResource(Name = "TypedProducts", PermissionPolicy = "Admin.TypedProducts")]
    private sealed class TypedRegistrationResource : AdminResource<RegistrationModel>
    {
        protected override void Configure(AdminResourceBuilder<RegistrationModel> resource)
        {
            resource.Field(model => model.Id);
        }
    }

    private sealed class RegistrationDbContext(
        DbContextOptions<RegistrationDbContext> options) : DbContext(options);

    private sealed class TestRequirement : IAuthorizationRequirement
    {
    }
}
