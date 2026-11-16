using Laranet.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages;

public sealed class IndexModel(
    IAdminResourceRegistry resources,
    IAdminAuthorization authorization) : PageModel
{
    public IReadOnlyList<AdminResource> Resources { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(
        CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        var decisions = await Task.WhenAll(resources.Resources.Select(async resource =>
            (Resource: resource, Allowed: await authorization.AuthorizeAsync(
                User,
                resource,
                AdminOperation.List,
                cancellationToken))));

        Resources = decisions
            .Where(decision => decision.Allowed)
            .Select(decision => decision.Resource)
            .ToArray();
        return Page();
    }
}
