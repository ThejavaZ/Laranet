using Laranet.Admin;
using Laranet.Admin.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace Laranet.Tests.Unit.Admin;

public class AdminAspNetCoreTests
{
    [Fact]
    public void AddLaranetAdmin_ShouldRegisterRegistryAndAuthorizationBridge()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddLaranetAdmin();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<AdminResourceRegistry>(
            provider.GetRequiredService<IAdminResourceRegistry>());
        Assert.NotNull(provider.GetRequiredService<IAdminAuthorization>());
    }
}
