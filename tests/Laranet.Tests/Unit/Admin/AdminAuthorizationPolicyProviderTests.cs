using Laranet.Admin;
using Laranet.Admin.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Laranet.Tests.Unit.Admin;

public class AdminAuthorizationPolicyProviderTests
{
    [Fact]
    public async Task OperationPolicy_ShouldSynthesizeFromRegisteredBase()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLaranetAdmin(options => options.AddResource(ProductsDefinition));

        using var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var basePolicy = await policyProvider.GetPolicyAsync("Admin.Products");
        var synthesized = await policyProvider.GetPolicyAsync("Admin.Products.Create");

        Assert.NotNull(basePolicy);
        Assert.NotNull(synthesized);
        // The synthesized policy clones the base requirements (same instances).
        Assert.Equal(basePolicy.Requirements, synthesized.Requirements);
    }

    [Fact]
    public async Task OperationPolicy_ExplicitRegistration_ShouldWin()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationBuilder()
            .AddPolicy("Admin.Products.Delete", policy =>
                policy.AddRequirements(new TestRequirement()));
        services.AddLaranetAdmin(options => options.AddResource(ProductsDefinition));

        using var provider = services.BuildServiceProvider();
        var policy = await provider
            .GetRequiredService<IAuthorizationPolicyProvider>()
            .GetPolicyAsync("Admin.Products.Delete");

        Assert.NotNull(policy);
        Assert.Contains(policy.Requirements, requirement => requirement is TestRequirement);
    }

    [Fact]
    public async Task OperationPolicy_UnknownBase_ShouldReturnNull()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLaranetAdmin(options => options.AddResource(ProductsDefinition));

        using var provider = services.BuildServiceProvider();
        var policy = await provider
            .GetRequiredService<IAuthorizationPolicyProvider>()
            .GetPolicyAsync("Unknown.Thing.Create");

        Assert.Null(policy);
    }

    [Fact]
    public async Task OperationPolicy_UnknownOperation_ShouldReturnNull()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLaranetAdmin(options => options.AddResource(ProductsDefinition));

        using var provider = services.BuildServiceProvider();
        var policy = await provider
            .GetRequiredService<IAuthorizationPolicyProvider>()
            .GetPolicyAsync("Admin.Products.Nope");

        Assert.Null(policy);
    }

    [Fact]
    public async Task DefaultPolicy_ShouldKeepStandardBehavior()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLaranetAdmin();

        using var provider = services.BuildServiceProvider();
        var policy = await provider
            .GetRequiredService<IAuthorizationPolicyProvider>()
            .GetDefaultPolicyAsync();

        Assert.NotNull(policy);
        Assert.NotEmpty(policy.Requirements);
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

    private sealed class TestRequirement : IAuthorizationRequirement
    {
    }
}
