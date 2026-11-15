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
        // In Development, allow access without authentication
        if (principal.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(
            string.Equals(resource.PermissionPolicy, "Admin.Products", StringComparison.Ordinal));
    }
}
