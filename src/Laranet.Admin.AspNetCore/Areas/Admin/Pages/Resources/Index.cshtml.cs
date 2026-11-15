using Laranet.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages.Resources;

public sealed class IndexModel(
    IAdminResourceRegistry resources,
    IAdminDataAdapter adapter,
    IAdminAuthorization authorization) : PageModel
{
    public AdminResource Resource { get; private set; } = null!;

    public AdminPage Results { get; private set; } = new([], 1, 25, 0);

    public bool SupportsBulk => adapter is IAdminEnhancementAdapter;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

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
                25,
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
