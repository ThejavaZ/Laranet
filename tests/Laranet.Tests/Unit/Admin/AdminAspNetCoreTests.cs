using System.Security.Claims;
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

    [Fact]
    public async Task Authorization_ShouldRequireAuthenticationAndOperationPermission()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLaranetAdmin();
        services.AddAuthorizationBuilder()
            .AddPolicy("Admin.Products.Update", policy =>
                policy.RequireClaim(AdminPermissionClaimTypes.Permission, "Admin.Products.Update"));

        using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAdminAuthorization>();
        var resource = new AdminResource(
            "Products",
            typeof(AuthProduct),
            [new AdminField("Id", "ID", typeof(int), IsKey: true)],
            [],
            [],
            PermissionPolicy: "Admin.Products");

        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        var authenticated = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(
                    AdminPermissionClaimTypes.Permission,
                    "Admin.Products.Update"),
            ],
            "test"));

        Assert.False(await authorization.AuthorizeAsync(
            anonymous,
            resource,
            AdminOperation.Update));
        Assert.True(await authorization.AuthorizeAsync(
            authenticated,
            resource,
            AdminOperation.Update));
        Assert.False(await authorization.AuthorizeAsync(
            authenticated,
            resource,
            AdminOperation.Delete));
    }

    private sealed class AuthProduct
    {
        public int Id { get; set; }
    }
}
