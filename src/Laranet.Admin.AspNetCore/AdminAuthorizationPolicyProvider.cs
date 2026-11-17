using Laranet.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Laranet.Admin.AspNetCore;

/// <summary>
/// Materializes per-operation admin policies (<c>Resource.Operation</c>) from a
/// registered base policy on demand (ADR-0016), so applications never register
/// them explicitly. Registered policies always win: an explicit registration
/// still specializes a single operation.
/// </summary>
internal sealed class AdminAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly IOptions<AuthorizationOptions> options;
    private readonly IAdminResourceRegistry registry;

    public AdminAuthorizationPolicyProvider(
        IOptions<AuthorizationOptions> options,
        IAdminResourceRegistry registry)
    {
        this.options = options;
        this.registry = registry;
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var authorization = options.Value;

        // Explicit registrations always win (ADR-0014/0015/0016).
        var registered = authorization.GetPolicy(policyName);
        if (registered is not null)
        {
            return Task.FromResult<AuthorizationPolicy?>(registered);
        }

        var separator = policyName.LastIndexOf('.');
        if (separator <= 0)
        {
            return Task.FromResult<AuthorizationPolicy?>(null);
        }

        var baseName = policyName[..separator];
        var operation = policyName[(separator + 1)..];

        // Only {Base}.{AdminOperation} names are candidates, and only when the
        // base matches a registered resource's permission policy.
        if (operation.Length == 0
            || !char.IsAsciiLetter(operation[0])
            || !Enum.TryParse<AdminOperation>(operation, out _)
            || !registry.Resources.Any(resource =>
                string.Equals(resource.PermissionPolicy, baseName, StringComparison.Ordinal)))
        {
            return Task.FromResult<AuthorizationPolicy?>(null);
        }

        var basePolicy = authorization.GetPolicy(baseName);
        return Task.FromResult(basePolicy is null
            ? null
            : new AuthorizationPolicy(
                basePolicy.Requirements,
                basePolicy.AuthenticationSchemes));
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => Task.FromResult(options.Value.DefaultPolicy);

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => Task.FromResult(options.Value.FallbackPolicy);
}
