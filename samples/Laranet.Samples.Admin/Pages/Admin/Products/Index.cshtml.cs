using Laranet.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Samples.Admin.Pages.Admin.Products;

[Authorize(Policy = "Admin.Products")]
public sealed class IndexModel(
    IAdminDataAdapter adapter,
    IAdminResourceRegistry resources) : PageModel
{
    public AdminPage Results { get; private set; } = new([], 1, 25, 0);

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Results = await adapter.ListAsync(
            resources.Get("Products"),
            new AdminQuery(PageNumber, 10, Search),
            cancellationToken);
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        await adapter.DeleteAsync(
            resources.Get("Products"),
            id,
            cancellationToken);

        return RedirectToPage();
    }
}
