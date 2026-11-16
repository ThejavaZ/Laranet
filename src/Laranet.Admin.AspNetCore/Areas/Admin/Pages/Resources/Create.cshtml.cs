using Laranet.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages.Resources;

public sealed class CreateModel(
    IAdminResourceRegistry resources,
    IAdminDataAdapter adapter,
    IAdminAuthorization authorization,
    IAdminAuditSink audit,
    IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly IStringLocalizer localizer =
        localizerFactory.Create(typeof(CreateModel));

    public AdminResource Resource { get; private set; } = null!;

    public bool Allowed { get; private set; }

    public IReadOnlyDictionary<string, IReadOnlyList<AdminLookupOption>> Lookups { get; private set; } =
        new Dictionary<string, IReadOnlyList<AdminLookupOption>>();

    public string L(AdminField field) =>
        AdminPageModelHelper.L(localizer, field);

    public IReadOnlyDictionary<string, string> Errors { get; private set; } =
        new Dictionary<string, string>();

    public async Task<IActionResult> OnGetAsync(
        string resource,
        CancellationToken cancellationToken)
    {
        if (!TryGetResource(resource, AdminOperation.Create))
        {
            return ChallengeOrForbid();
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
            return ChallengeOrForbid();
        }

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(cancellationToken);
            return Page();
        }

        try
        {
            await adapter.CreateAsync(Resource, ReadValues(), cancellationToken);
            await AdminAudit.WriteSafeAsync(
                audit,
                HttpContext,
                "create",
                "success",
                Resource,
                AdminOperation.Create,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await AdminAudit.WriteSafeAsync(
                audit,
                HttpContext,
                "create",
                "failure",
                Resource,
                AdminOperation.Create,
                ex.GetType().Name,
                cancellationToken);
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadLookupsAsync(cancellationToken);
            return Page();
        }

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
