using Laranet.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages.Resources;

public sealed class CreateModel(
    IAdminResourceRegistry resources,
    IAdminDataAdapter adapter,
    IAdminAuthorization authorization) : PageModel
{
    public AdminResource Resource { get; private set; } = null!;

    public bool Allowed { get; private set; }

    public IActionResult OnGet(string resource)
    {
        if (!TryGetResource(resource, AdminOperation.Create))
        {
            return NotFound();
        }

        return Page();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(
        string resource,
        CancellationToken cancellationToken)
    {
        if (!TryGetResource(resource, AdminOperation.Create))
        {
            return NotFound();
        }

        await adapter.CreateAsync(Resource, ReadValues(), cancellationToken);

        return RedirectToPage(
            "/Resources/Index",
            new
            {
                resource,
                adminMessage = "Record created successfully.",
            });
    }

    private bool TryGetResource(string name, AdminOperation operation)
    {
        try
        {
            Resource = resources.Get(name);
        }
        catch (KeyNotFoundException)
        {
            return false;
        }

        Allowed = authorization
            .AuthorizeAsync(User, Resource, operation)
            .GetAwaiter()
            .GetResult();

        return Allowed;
    }

    private Dictionary<string, object?> ReadValues()
    {
        return Resource.Form?.Fields.ToDictionary(
                   field => field.Name,
                   field => (object?)Request.Form[field.Name].FirstOrDefault())
               ?? [];
    }
}
