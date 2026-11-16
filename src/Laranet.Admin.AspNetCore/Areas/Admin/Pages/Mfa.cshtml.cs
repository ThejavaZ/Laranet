using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages;

[AllowAnonymous]
public sealed class MfaModel(
    IAdminUserStore users,
    IAdminMfaProvider mfa,
    IAdminAuditSink audit) : PageModel
{
    [BindProperty]
    public string Code { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet() =>
        TempData.ContainsKey("AdminMfaUser") ? Page() : RedirectToPage("/Login");

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userName = TempData["AdminMfaUser"] as string;
        var returnUrl = TempData["AdminMfaReturnUrl"] as string ?? "/admin";
        var user = userName is null
            ? null
            : await users.FindByUserNameAsync(userName, cancellationToken);

        var valid = user is not null &&
            user.IsActive &&
            user.MfaEnabled &&
            !string.IsNullOrWhiteSpace(user.MfaSecret) &&
            mfa.ValidateCode(user.MfaSecret, Code);

        if (!valid)
        {
            TempData["AdminMfaUser"] = userName;
            TempData["AdminMfaReturnUrl"] = returnUrl;
            await AdminAudit.WriteSafeAsync(
                audit,
                HttpContext,
                "mfa",
                "failure",
                details: "Invalid verification code",
                cancellationToken: cancellationToken);
            ModelState.AddModelError(string.Empty, "Invalid verification code.");
            return Page();
        }

        await AdminSignIn.SignInAsync(HttpContext, user!);
        await AdminAudit.WriteSafeAsync(
            audit,
            HttpContext,
            "mfa",
            "success",
            cancellationToken: cancellationToken);
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/admin");
    }
}
