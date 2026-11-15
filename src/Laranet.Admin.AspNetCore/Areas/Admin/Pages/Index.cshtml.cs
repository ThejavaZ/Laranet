using Laranet.Admin;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages;

public sealed class IndexModel(IAdminResourceRegistry resources) : PageModel
{
    public IReadOnlyList<AdminResource> Resources { get; private set; } = [];

    public void OnGet()
    {
        Resources = resources.Resources;
    }
}
