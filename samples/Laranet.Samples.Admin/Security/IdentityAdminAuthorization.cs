using System.Security.Claims;
using Laranet.Admin;

namespace Laranet.Samples.Admin.Security;

public sealed class IdentityAdminAuthorization : IAdminAuthorization
{
    public Task<bool> AuthorizeAsync(
        ClaimsPrincipal principal,
        AdminResource resource,
        AdminOperation operation,
        CancellationToken cancellationToken = default)
    {
        var allowed = principal.Identity?.IsAuthenticated == true
            && string.Equals(resource.PermissionPolicy, "Admin.Products", StringComparison.Ordinal);

        return Task.FromResult(allowed);
    }
}
