using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages;

[AllowAnonymous]
public sealed class MfaModel(
    IAdminUserStore users,
    IAdminMfaProvider mfa,
    IAdminPasswordHasher passwordHasher,
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

        var validTotp = user is not null &&
            user.IsActive &&
            user.MfaEnabled &&
            !string.IsNullOrWhiteSpace(user.MfaSecret) &&
            mfa.ValidateCode(user.MfaSecret, Code);

        var recoveryCodeIndex = -1;
        if (user?.MfaRecoveryCodeHashes is { Count: > 0 } recoveryCodes)
        {
            for (var index = 0; index < recoveryCodes.Count; index++)
            {
                if (passwordHasher.Verify(Code, recoveryCodes[index]))
                {
                    recoveryCodeIndex = index;
                    break;
                }
            }
        }
        var validRecovery = user is not null && recoveryCodeIndex >= 0;

        if (!validTotp && !validRecovery)
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

        if (validRecovery)
        {
            var remainingCodes = user!.MfaRecoveryCodeHashes!
                .Where((_, index) => index != recoveryCodeIndex)
                .ToArray();
            user = user with
            {
                MfaRecoveryCodeHashes = remainingCodes,
                SecurityStamp = Guid.NewGuid().ToString("N"),
            };
            await users.SaveAsync(user, cancellationToken);
        }

        await AdminSignIn.SignInAsync(HttpContext, user!);
        await AdminAudit.WriteSafeAsync(
            audit,
            HttpContext,
            validRecovery ? "mfa-recovery-code" : "mfa",
            "success",
            cancellationToken: cancellationToken);
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/admin");
    }
}
