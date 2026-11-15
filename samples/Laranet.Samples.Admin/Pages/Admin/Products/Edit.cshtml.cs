using Laranet.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Samples.Admin.Pages.Admin.Products;

[Authorize(Policy = "Admin.Products")]
public sealed class EditModel(
    IAdminDataAdapter adapter,
    IAdminResourceRegistry resources) : PageModel
{
    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public decimal Price { get; set; }

    [BindProperty]
    public bool IsActive { get; set; }

    public async Task<IActionResult> OnGetAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var row = await adapter.ReadAsync(
            resources.Get("Products"),
            id,
            cancellationToken);

        if (row is null)
        {
            return NotFound();
        }

        Load(row);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError(nameof(Name), "Name is required.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await adapter.UpdateAsync(
            resources.Get("Products"),
            Id,
            new Dictionary<string, object?>
            {
                ["Name"] = Name,
                ["Price"] = Price,
                ["IsActive"] = IsActive,
            },
            cancellationToken);

        return RedirectToPage("./Index");
    }

    private void Load(IReadOnlyDictionary<string, object?> row)
    {
        Id = Convert.ToInt32(row["Id"]);
        Name = Convert.ToString(row["Name"]) ?? string.Empty;
        Price = Convert.ToDecimal(row["Price"]);
        IsActive = Convert.ToBoolean(row["IsActive"]);
    }
}
