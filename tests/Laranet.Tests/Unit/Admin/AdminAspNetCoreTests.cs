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

    [Fact]
    public async Task AddLaranetAdminAuthentication_ShouldRegisterStandaloneCookieAndUserStore()
    {
        var services = new ServiceCollection();

        services.AddLaranetAdminAuthentication(options =>
            options.AddUser("admin", "correct horse battery staple"));

        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IAdminUserStore>();
        var hasher = provider.GetRequiredService<IAdminPasswordHasher>();
        var user = await store.FindByUserNameAsync("ADMIN");

        Assert.NotNull(user);
        Assert.True(user.IsActive);
        Assert.True(hasher.Verify("correct horse battery staple", user.PasswordHash));
        Assert.False(hasher.Verify("wrong password", user.PasswordHash));
        Assert.NotNull(provider.GetRequiredService<
            Microsoft.AspNetCore.Authentication.IAuthenticationService>());

        await store.SaveAsync(user with { IsActive = false });
        Assert.False((await store.FindByUserNameAsync("admin"))!.IsActive);
    }

    [Fact]
    public void AddLaranetAdminAuthentication_ShouldLockRepeatedFailures()
    {
        var services = new ServiceCollection();

        services.AddLaranetAdminAuthentication(options =>
        {
            options.MaxFailedAttempts = 2;
            options.LockoutDuration = TimeSpan.FromMinutes(1);
        });

        using var provider = services.BuildServiceProvider();
        var limiter = provider.GetRequiredService<IAdminLoginRateLimiter>();

        limiter.RegisterFailure("client|admin");
        Assert.True(limiter.IsAllowed("client|admin"));
        limiter.RegisterFailure("client|admin");

        Assert.False(limiter.IsAllowed("client|admin"));
        limiter.Reset("client|admin");
        Assert.True(limiter.IsAllowed("client|admin"));
    }
}
