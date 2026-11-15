using Laranet.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages.Resources;

public sealed class EditModel(
    IAdminResourceRegistry resources,
    IAdminDataAdapter adapter,
    IAdminAuthorization authorization,
    Microsoft.Extensions.Localization.IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly Microsoft.Extensions.Localization.IStringLocalizer localizer =
        localizerFactory.Create(typeof(EditModel));

    public AdminResource Resource { get; private set; } = null!;

    public IReadOnlyDictionary<string, object?> Values { get; private set; } = null!;

    public string L(AdminField field) =>
        field.LocalizationKey is not null
            ? localizer[field.LocalizationKey]
            : field.Label;

    public IReadOnlyDictionary<string, IReadOnlyList<AdminLookupOption>> Lookups { get; private set; } = new Dictionary<string, IReadOnlyList<AdminLookupOption>>();

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

        if (Values.Count == 0)
        {
            return NotFound();
        }

        await LoadLookupsAsync(cancellationToken);

        return Page();
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

    private async Task LoadLookupsAsync(CancellationToken cancellationToken)
    {
        if (adapter is not IAdminEnhancementAdapter enhancements)
        {
            return;
        }

        Lookups = (await Task.WhenAll(
                Resource.RelationDefinitions.Select(async relation =>
                    (relation.Name, Values: await enhancements.GetLookupAsync(
                        Resource,
                        relation,
                        null,
                        cancellationToken))))
            .ConfigureAwait(false))
            .ToDictionary(item => item.Name, item => item.Values);
    }
}
