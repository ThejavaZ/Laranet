using Laranet.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages.Resources;

public sealed class DetailsModel(
    IAdminResourceRegistry resources,
    IAdminDataAdapter adapter,
    IAdminAuthorization authorization,
    IAdminAuditSink audit,
    IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly IStringLocalizer localizer =
        localizerFactory.Create(typeof(DetailsModel));

    public AdminResource Resource { get; private set; } = null!;

    public IReadOnlyDictionary<string, object?> Values { get; private set; } =
        new Dictionary<string, object?>();

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

        await AdminAudit.WriteSafeAsync(
            audit,
            HttpContext,
            "read",
            "success",
            Resource,
            AdminOperation.Read,
            key,
            cancellationToken);

        return Page();
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
}
