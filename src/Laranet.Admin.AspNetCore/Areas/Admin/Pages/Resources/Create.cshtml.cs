using Laranet.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages.Resources;

public sealed class CreateModel(
    IAdminResourceRegistry resources,
    IAdminDataAdapter adapter,
    IAdminAuthorization authorization,
    Microsoft.Extensions.Localization.IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly Microsoft.Extensions.Localization.IStringLocalizer localizer =
        localizerFactory.Create(typeof(CreateModel));

    public AdminResource Resource { get; private set; } = null!;

    public bool Allowed { get; private set; }

    public string L(AdminField field) =>
        field.LocalizationKey is not null
            ? localizer[field.LocalizationKey]
            : field.Label;

    public IReadOnlyDictionary<string, IReadOnlyList<AdminLookupOption>> Lookups { get; private set; } = new Dictionary<string, IReadOnlyList<AdminLookupOption>>();

    public async Task<IActionResult> OnGetAsync(
        string resource,
        CancellationToken cancellationToken)
    {
        if (!TryGetResource(resource, AdminOperation.Create))
        {
            return NotFound();
        }

        await LoadLookupsAsync(cancellationToken);

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
