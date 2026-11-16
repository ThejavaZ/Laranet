using Laranet.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages.Resources;

public sealed class IndexModel(
    IAdminResourceRegistry resources,
    IAdminDataAdapter adapter,
    IAdminAuthorization authorization,
    IStringLocalizerFactory localizerFactory) : PageModel
{
    private readonly IStringLocalizer localizer =
        localizerFactory.Create(typeof(IndexModel));

    public AdminResource Resource { get; private set; } = null!;

    public AdminPage Results { get; private set; } = new([], 1, 25, 0);

    public Dictionary<string, string> RouteValues { get; private set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public AdminTableModel Table => new(
        Resource,
        Results,
        SortBy,
        SortDescending,
        SupportsBulk,
        RouteValues,
        f => L(f),
        FormatValue);

    public AdminPaginationModel Pagination => new(Results, RouteValues);

    public string L(AdminField field) =>
        AdminPageModelHelper.L(localizer, field);

    public string L(AdminFilter filter) =>
        AdminPageModelHelper.L(localizer, filter);

    public string L(AdminAction action) =>
        AdminPageModelHelper.L(localizer, action);

    public string FormatValue(AdminField field, object? value) =>
        AdminPageModelHelper.FormatValue(field, value);

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

    public IReadOnlyDictionary<string, string?> Filters { get; private set; } =
        new Dictionary<string, string?>();

    public async Task<IActionResult> OnGetAsync(
        string resource,
        CancellationToken cancellationToken)
    {
        if (!TryGetResource(resource, AdminOperation.List))
        {
            return ChallengeOrForbid();
        }

        Resource = resources.Get(resource);
        Filters = AdminPageModelHelper.ReadFilters(Resource, Request);

        // Validate SortBy against known fields
        if (!string.IsNullOrWhiteSpace(SortBy) &&
            !Resource.OrderableFields.Any(f => f.Name == SortBy))
        {
            SortBy = null;
        }

        // Clamp pagination
        PageNumber = Math.Max(1, PageNumber);
        PageSize = Math.Clamp(PageSize, 1, 100);

        Results = await adapter.ListAsync(
            Resource,
            new AdminQuery(
                PageNumber,
                PageSize,
                Search,
                Filters,
                SortBy,
                SortDescending),
            cancellationToken);

        RouteValues = BuildRouteValues();

        return Page();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostBulkAsync(
        string resource,
        string action,
        string[]? keys,
        CancellationToken cancellationToken)
    {
        if (!TryGetResource(resource, AdminOperation.BulkAction))
        {
            return ChallengeOrForbid();
        }

        if (keys is null || keys.Length == 0)
        {
            TempData["AdminError"] = "No records selected.";
            return RedirectToPage(new { resource, adminMessage = "No records selected." });
        }

        if (adapter is not IAdminEnhancementAdapter enhancements)
        {
            return NotFound();
        }

        var definition = resources.Get(resource);
        var count = await enhancements.ExecuteBulkActionAsync(
            definition,
            action,
            keys.Cast<object>().ToArray(),
            cancellationToken);

        return RedirectToPage(new
        {
            resource,
            adminMessage = $"{count} record(s) updated successfully.",
        });
    }

    public async Task<IActionResult> OnGetExportAsync(
        string resource,
        CancellationToken cancellationToken)
    {
        if (!TryGetResource(resource, AdminOperation.List))
        {
            return ChallengeOrForbid();
        }

        var definition = resources.Get(resource);
        var all = await adapter.ListAsync(
            definition,
            new AdminQuery(1, 10000, Search, Filters, SortBy, SortDescending),
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

        var bytes = new System.Text.UTF8Encoding(true).GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"{definition.Name}.csv");
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostDeleteAsync(
        string resource,
        string key,
        CancellationToken cancellationToken)
    {
        if (!TryGetResource(resource, AdminOperation.Delete))
        {
            return ChallengeOrForbid();
        }

        var definition = resources.Get(resource);
        await adapter.DeleteAsync(definition, key, cancellationToken);

        return RedirectToPage(new
        {
            resource,
            adminMessage = "Record deleted successfully.",
        });
    }

    private bool TryGetResource(
        string name,
        AdminOperation operation)
    {
        AdminResource? definition;
        try
        {
            definition = resources.Get(name);
        }
        catch (KeyNotFoundException)
        {
            return false;
        }

        var result = authorization
            .AuthorizeAsync(User, definition, operation)
            .GetAwaiter()
            .GetResult();

        if (!result)
        {
            return false;
        }

        Resource = definition;
        return true;
    }

    private IActionResult ChallengeOrForbid()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }
        return Forbid();
    }

    private Dictionary<string, string> BuildRouteValues()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["resource"] = Resource.Name,
            ["PageSize"] = PageSize.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            ["SortDescending"] = SortDescending.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(Search))
        {
            values["Search"] = Search;
        }

        if (!string.IsNullOrWhiteSpace(SortBy))
        {
            values["SortBy"] = SortBy;
        }

        foreach (var filter in Filters)
        {
            if (!string.IsNullOrEmpty(filter.Value))
            {
                values[filter.Key] = filter.Value;
            }
        }

        return values;
    }

    private IReadOnlyDictionary<string, string?> ReadFilters()
    {
        return AdminPageModelHelper.ReadFilters(Resource, Request);
    }
}
