using Laranet.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages.Resources;

public sealed class DetailsModel(
    IAdminResourceRegistry resources,
    IAdminDataAdapter adapter,
    IAdminAuthorization authorization,
    Microsoft.Extensions.Localization.IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly Microsoft.Extensions.Localization.IStringLocalizer localizer =
        localizerFactory.Create(typeof(DetailsModel));

    public AdminResource Resource { get; private set; } = null!;

    public IReadOnlyDictionary<string, object?> Values { get; private set; } =
        new Dictionary<string, object?>();

    public string L(AdminField field) =>
        field.LocalizationKey is not null
            ? localizer[field.LocalizationKey]
            : field.Label;

    public string FormatValue(AdminField field, object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return field.Format switch
        {
            "currency" => string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                "{0:C}",
                value),
            "date" => value is DateTime dt
                ? dt.ToString("d", System.Globalization.CultureInfo.CurrentCulture)
                : value.ToString() ?? string.Empty,
            "datetime" => value is DateTime dt2
                ? dt2.ToString("g", System.Globalization.CultureInfo.CurrentCulture)
                : value.ToString() ?? string.Empty,
            "number" => string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                "{0:N}",
                value),
            _ => value.ToString() ?? string.Empty,
        };
    }

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
}
