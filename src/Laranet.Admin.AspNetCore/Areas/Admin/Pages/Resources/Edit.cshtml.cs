using Laranet.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages.Resources;

public sealed class EditModel(
    IAdminResourceRegistry resources,
    IAdminDataAdapter adapter,
    IAdminAuthorization authorization) : PageModel
{
    public AdminResource Resource { get; private set; } = null!;

    public IReadOnlyDictionary<string, object?> Values { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(
        string resource,
        string key,
        CancellationToken cancellationToken)
    {
        if (!TryGetResource(resource, AdminOperation.Read))
        {
            return NotFound();
        }

        Values = await adapter.ReadAsync(Resource, key, cancellationToken)
            ?? new Dictionary<string, object?>();

        return Values.Count == 0 ? NotFound() : Page();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(
        string resource,
        string key,
        CancellationToken cancellationToken)
    {
        if (!TryGetResource(resource, AdminOperation.Update))
        {
            return NotFound();
        }

        await adapter.UpdateAsync(Resource, key, ReadValues(), cancellationToken);

        return RedirectToPage(
            "/Resources/Index",
            new
            {
                resource,
                adminMessage = "Record updated successfully.",
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

        return authorization
            .AuthorizeAsync(User, Resource, operation)
            .GetAwaiter()
            .GetResult();
    }

    private Dictionary<string, object?> ReadValues()
    {
        return Resource.Form?.Fields.ToDictionary(
                   field => field.Name,
                   field => (object?)Request.Form[field.Name].FirstOrDefault())
               ?? [];
    }
}
