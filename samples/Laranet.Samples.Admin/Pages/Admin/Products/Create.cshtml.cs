using Laranet.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Samples.Admin.Pages.Admin.Products;

[Authorize(Policy = "Admin.Products")]
public sealed class CreateModel(
    IAdminDataAdapter adapter,
    IAdminResourceRegistry resources) : PageModel
{
    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public decimal Price { get; set; }

    [BindProperty]
    public bool IsActive { get; set; } = true;

    public void OnGet()
    {
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

        await adapter.CreateAsync(
            resources.Get("Products"),
            new Dictionary<string, object?>
            {
                ["Name"] = Name,
                ["Price"] = Price,
                ["IsActive"] = IsActive,
            },
            cancellationToken);

        return RedirectToPage("./Index");
    }
}
