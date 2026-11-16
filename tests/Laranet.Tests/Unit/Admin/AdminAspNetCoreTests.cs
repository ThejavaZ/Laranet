using System.Security.Claims;
using Laranet.Admin;
using Laranet.Admin.AspNetCore;
using Laranet.Admin.AspNetCore.Areas.Admin.Pages;
using Laranet.Admin.AspNetCore.Areas.Admin.Pages.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ResourcesIndexModel = Laranet.Admin.AspNetCore.Areas.Admin.Pages.Resources.IndexModel;

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
        Assert.NotNull(provider.GetRequiredService<IAdminAuditSink>());
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

    [Fact]
    public void MutationHandlers_ShouldRequireAntiforgery()
    {
        var handlers = new[]
        {
            typeof(CreateModel).GetMethod(nameof(CreateModel.OnPostAsync)),
            typeof(EditModel).GetMethod(nameof(EditModel.OnPostAsync)),
            typeof(ResourcesIndexModel).GetMethod(nameof(ResourcesIndexModel.OnPostDeleteAsync)),
            typeof(ResourcesIndexModel).GetMethod(nameof(ResourcesIndexModel.OnPostBulkAsync)),
            typeof(LogoutModel).GetMethod(nameof(LogoutModel.OnPostAsync)),
            typeof(PasswordModel).GetMethod(nameof(PasswordModel.OnPostAsync)),
        };

        Assert.All(
            handlers,
            handler => Assert.NotNull(
                handler!.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true)
                    .SingleOrDefault()));
    }

    [Fact]
    public async Task Authentication_ShouldProvideSingleUseResetTokensAndTotpSetup()
    {
        var services = new ServiceCollection();
        services.AddLaranetAdminAuthentication();

        using var provider = services.BuildServiceProvider();
        var tokens = provider.GetRequiredService<IAdminPasswordResetTokenStore>();
        var mfa = provider.GetRequiredService<IAdminMfaProvider>();
        var token = await tokens.CreateAsync(
            "admin",
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal("admin", await tokens.ConsumeAsync(token));
        Assert.Null(await tokens.ConsumeAsync(token));

        var secret = mfa.CreateSecret();
        Assert.Equal(32, secret.Length);
        Assert.StartsWith("otpauth://totp/", mfa.CreateProvisioningUri("admin", secret));
        Assert.False(mfa.ValidateCode(secret, "000000"));
    }

    private sealed class AuthProduct
    {
        public int Id { get; set; }
    }
}
