using Laranet.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages.Resources;

public sealed class IndexModel(
    IAdminResourceRegistry resources,
    IAdminDataAdapter adapter,
    IAdminAuthorization authorization,
    Microsoft.Extensions.Localization.IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly Microsoft.Extensions.Localization.IStringLocalizer localizer =
        localizerFactory.Create(typeof(IndexModel));

    public AdminResource Resource { get; private set; } = null!;

    public AdminPage Results { get; private set; } = new([], 1, 25, 0);

    public string L(AdminField field) =>
        field.LocalizationKey is not null
            ? localizer[field.LocalizationKey]
            : field.Label;

    public string L(AdminFilter filter) =>
        filter.LocalizationKey is not null
            ? localizer[filter.LocalizationKey]
            : filter.Label;

    public string L(AdminAction action) =>
        action.LocalizationKey is not null
            ? localizer[action.LocalizationKey]
            : action.Label;

    public bool SupportsBulk => adapter is IAdminEnhancementAdapter;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 25;

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool SortDescending { get; set; }

    public async Task<IActionResult> OnGetAsync(
        string resource,
        CancellationToken cancellationToken)
    {
        if (!TryGetResource(resource, AdminOperation.List, out var definition))
        {
            return NotFound();
        }

        Resource = definition;
        Results = await adapter.ListAsync(
            definition,
            new AdminQuery(
                PageNumber,
                PageSize,
                Search,
                ReadFilters(),
                SortBy,
                SortDescending),
            cancellationToken);

        return Page();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostBulkAsync(
        string resource,
        string action,
        string[] keys,
        CancellationToken cancellationToken)
    {
        if (!TryGetResource(resource, AdminOperation.BulkAction, out var definition)
            || adapter is not IAdminEnhancementAdapter enhancements)
        {
            return NotFound();
        }

        await enhancements.ExecuteBulkActionAsync(
            definition,
            action,
            keys.Cast<object>().ToArray(),
            cancellationToken);

        return RedirectToPage(new
        {
            resource,
            adminMessage = "Selected records updated successfully.",
        });
    }

    public async Task<IActionResult> OnGetExportAsync(
        string resource,
        CancellationToken cancellationToken)
    {
        if (!TryGetResource(resource, AdminOperation.List, out var definition))
        {
            return NotFound();
        }

        var all = await adapter.ListAsync(
            definition,
            new AdminQuery(1, 10000, Search, ReadFilters(), SortBy, SortDescending),
            cancellationToken);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(string.Join(",", definition.Fields.Select(f => $"\"{f.Label}\"")));

        foreach (var item in all.Items)
        {
            var values = definition.Fields.Select(f =>
            {
                var val = item.GetValueOrDefault(f.Name);
                return $"\"{val?.ToString()?.Replace("\"", "\"\"")}\"";
            });
            sb.AppendLine(string.Join(",", values));
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"{definition.Name}.csv");
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostDeleteAsync(
        string resource,
        string key,
        CancellationToken cancellationToken)
    {
        if (!TryGetResource(resource, AdminOperation.Delete, out var definition))
        {
            return NotFound();
        }

        await adapter.DeleteAsync(definition, key, cancellationToken);

        return RedirectToPage(new
        {
            resource,
            adminMessage = "Record deleted successfully.",
        });
    }

    private bool TryGetResource(
        string name,
        AdminOperation operation,
        out AdminResource definition)
    {
        try
        {
            definition = resources.Get(name);
        }
        catch (KeyNotFoundException)
        {
            definition = null!;
            return false;
        }

        return authorization
            .AuthorizeAsync(User, definition, operation)
            .GetAwaiter()
            .GetResult();
    }

    private IReadOnlyDictionary<string, string?> ReadFilters()
    {
        return Resource.Filters
            .Select(filter => new
            {
                filter.Name,
                Value = Request.Query[filter.Name].FirstOrDefault(),
            })
            .Where(item => item.Value is not null)
            .ToDictionary(item => item.Name, item => item.Value);
    }
}
