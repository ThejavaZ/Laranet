using Laranet.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Laranet.Admin.AspNetCore;

/// <summary>
/// Registers the experimental Laranet Admin Razor Pages integration.
/// </summary>
public static class AdminAspNetCoreExtensions
{
    /// <summary>
    /// Registers the Admin resource registry and native ASP.NET Core authorization
    /// bridge. Call <c>MapRazorPages</c> on the application to expose the pages.
    /// </summary>
    public static IServiceCollection AddLaranetAdmin(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorization();
        services.AddLocalization();
        services.TryAddSingleton<IAdminResourceRegistry, AdminResourceRegistry>();
        services.TryAddScoped<IAdminAuthorization, AspNetCoreAdminAuthorization>();

        return services;
    }
}

internal sealed class AspNetCoreAdminAuthorization(
    IAuthorizationService authorization)
    : IAdminAuthorization
{
    public async Task<bool> AuthorizeAsync(
        System.Security.Claims.ClaimsPrincipal principal,
        AdminResource resource,
        AdminOperation operation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resource.PermissionPolicy))
        {
            return principal.Identity?.IsAuthenticated == true;
        }

        var result = await authorization.AuthorizeAsync(
            principal,
            resource.PermissionPolicy);

        return result.Succeeded;
    }
}
