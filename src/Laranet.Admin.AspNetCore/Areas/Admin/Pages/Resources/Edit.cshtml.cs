using Laranet.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages.Resources;

public sealed class EditModel(
    IAdminResourceRegistry resources,
    IAdminDataAdapter adapter,
    IAdminAuthorization authorization,
    IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly IStringLocalizer localizer =
        localizerFactory.Create(typeof(EditModel));

    public AdminResource Resource { get; private set; } = null!;

    public IReadOnlyDictionary<string, object?> Values { get; private set; } =
        new Dictionary<string, object?>();

    public IReadOnlyDictionary<string, IReadOnlyList<AdminLookupOption>> Lookups { get; private set; } =
        new Dictionary<string, IReadOnlyList<AdminLookupOption>>();

    public string L(AdminField field) =>
        AdminPageModelHelper.L(localizer, field);

    public string FormatValue(AdminField field, object? value) =>
        AdminPageModelHelper.FormatValue(field, value);

    public async Task<IActionResult> OnGetAsync(
        string resource,
        string key,
        CancellationToken cancellationToken)
    {
        if (!TryGetResource(resource, AdminOperation.Read))
        {
            return ChallengeOrForbid();
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
            return ChallengeOrForbid();
        }

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(cancellationToken);
            return Page();
        }

        try
        {
            await adapter.UpdateAsync(Resource, key, ReadValues(), cancellationToken);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadLookupsAsync(cancellationToken);
            return Page();
        }

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

    private IActionResult ChallengeOrForbid()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }
        return Forbid();
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

    private Dictionary<string, object?> ReadValues()
    {
        var fields = Resource.Form?.Fields ?? Resource.Fields;
        return fields.ToDictionary(
            field => field.Name,
            field => (object?)Request.Form[field.Name].FirstOrDefault());
    }
}
