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

        // Registered before AddAuthorization so the admin provider occupies the
        // slot the default one would take. TryAdd: an application-provided
        // provider registered first still wins (ADR-0016).
        services.TryAddSingleton<
            IAuthorizationPolicyProvider,
            AdminAuthorizationPolicyProvider>();
        services.AddAuthorization();
        services.AddLocalization();
        services.TryAddSingleton<IAdminResourceRegistry, AdminResourceRegistry>();
        services.TryAddSingleton<IAdminAuditSink, NullAdminAuditSink>();
        services.TryAddScoped<IAdminAuthorization, AspNetCoreAdminAuthorization>();

        return services;
    }

    /// <summary>
    /// Registers the experimental Laranet Admin Razor Pages integration from a
    /// declarative configuration: explicit resources, discovery sources, data
    /// adapter factories, and default base policies. Base policies named by a
    /// resource's <c>PermissionPolicy</c> are registered only when the
    /// application has not registered them, so explicit policies always win.
    /// Call <c>MapRazorPages</c> on the application to expose the pages.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The registration inputs (see ADR-0014).</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddLaranetAdmin(
        this IServiceCollection services,
        Action<AdminRegistrationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddLaranetAdmin();

        var options = new AdminRegistrationOptions();
        configure(options);

        var registry = new AdminResourceRegistry();
        foreach (var definition in options.Resources)
        {
            registry.Add(definition);
        }

        foreach (var assembly in options.DiscoveryAssemblies)
        {
            AdminResourceDiscovery.RegisterAll(registry, assembly);
        }

        // The instance registry replaces the empty one from the base overload:
        // the last registration wins when the service is resolved.
        services.AddSingleton<IAdminResourceRegistry>(registry);

        foreach (var factory in options.AdapterFactories)
        {
            services.AddScoped<IAdminDataAdapter>(factory);
        }

        var policyNames = registry.Resources
            .Select(resource => resource.PermissionPolicy)
            .OfType<string>()
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Distinct(StringComparer.Ordinal);

        foreach (var policyName in policyNames)
        {
            services.Configure<AuthorizationOptions>(authorization =>
            {
                if (authorization.GetPolicy(policyName) is null)
                {
                    authorization.AddPolicy(
                        policyName,
                        policy => policy.RequireAuthenticatedUser());
                }
            });
        }

        return services;
    }
}

internal sealed class AspNetCoreAdminAuthorization(
    IAuthorizationService authorization,
    IAuthorizationPolicyProvider policies)
    : IAdminAuthorization
{
    public async Task<bool> AuthorizeAsync(
        System.Security.Claims.ClaimsPrincipal principal,
        AdminResource resource,
        AdminOperation operation,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var operationPolicy = resource.PermissionFor(operation);
        if (operationPolicy is null)
        {
            return false;
        }

        if (principal.HasClaim(
                AdminPermissionClaimTypes.Permission,
                operationPolicy))
        {
            return true;
        }

        var operationDefinition = await policies.GetPolicyAsync(operationPolicy);
        if (operationDefinition is not null)
        {
            var operationResult = await authorization.AuthorizeAsync(
                principal,
                operationDefinition);
            return operationResult.Succeeded;
        }

        var baseDefinition = await policies.GetPolicyAsync(resource.PermissionPolicy!);
        if (baseDefinition is null)
        {
            return false;
        }

        var result = await authorization.AuthorizeAsync(principal, baseDefinition);

        return result.Succeeded;
    }
}
