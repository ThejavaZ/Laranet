using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages;

[AllowAnonymous]
public sealed class LogoutModel(IAdminAuditSink audit) : PageModel
{
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(AdminAuthenticationDefaults.Scheme);
        await AdminAudit.WriteSafeAsync(
            audit,
            HttpContext,
            "logout",
            "success");
        return RedirectToPage("/Login");
    }
}
