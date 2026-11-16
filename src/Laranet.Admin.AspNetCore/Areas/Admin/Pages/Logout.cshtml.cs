using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages;

[AllowAnonymous]
public sealed class LogoutModel : PageModel
{
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(AdminAuthenticationDefaults.Scheme);
        return RedirectToPage("/Login");
    }
}
