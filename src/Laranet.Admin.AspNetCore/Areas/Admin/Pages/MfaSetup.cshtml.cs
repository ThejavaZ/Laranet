using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages;

[Authorize(AuthenticationSchemes = AdminAuthenticationDefaults.Scheme)]
public sealed class MfaSetupModel(
    IAdminUserStore users,
    IAdminMfaProvider mfa,
    IAdminPasswordHasher passwordHasher,
    IAdminAuditSink audit) : PageModel
{
    [BindProperty]
    public string Code { get; set; } = string.Empty;

    public string Secret { get; private set; } = string.Empty;

    public string ProvisioningUri { get; private set; } = string.Empty;

    public IReadOnlyList<string> RecoveryCodes { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Challenge();
        }

        if (user.MfaEnabled && !string.IsNullOrWhiteSpace(user.MfaSecret))
        {
            Secret = user.MfaSecret;
            ProvisioningUri = mfa.CreateProvisioningUri(user.UserName, user.MfaSecret);
            return Page();
        }

        Secret = mfa.CreateSecret();
        ProvisioningUri = mfa.CreateProvisioningUri(user.UserName, Secret);
        TempData["AdminMfaSetupSecret"] = Secret;
        return Page();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var user = await CurrentUserAsync(cancellationToken);
        var secret = TempData["AdminMfaSetupSecret"] as string;
        if (user is null || string.IsNullOrWhiteSpace(secret) ||
            !mfa.ValidateCode(secret, Code))
        {
            ModelState.AddModelError(string.Empty, "Invalid verification code.");
            Secret = secret ?? string.Empty;
            ProvisioningUri = Secret.Length == 0
                ? string.Empty
                : mfa.CreateProvisioningUri(user?.UserName ?? string.Empty, Secret);
            TempData["AdminMfaSetupSecret"] = Secret;
            return Page();
        }

        RecoveryCodes = mfa.CreateRecoveryCodes();
        var recoveryCodeHashes = RecoveryCodes
            .Select(passwordHasher.Hash)
            .ToArray();

        await users.SaveAsync(
            user with
            {
                MfaEnabled = true,
                MfaSecret = secret,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                MfaRecoveryCodeHashes = recoveryCodeHashes,
            },
            cancellationToken);
        await AdminAudit.WriteSafeAsync(
            audit,
            HttpContext,
            "mfa-setup",
            "success",
            cancellationToken: cancellationToken);
        await HttpContext.SignOutAsync(AdminAuthenticationDefaults.Scheme);
        return Page();
    }

    private Task<AdminUser?> CurrentUserAsync(CancellationToken cancellationToken) =>
        User.Identity?.Name is string userName
            ? users.FindByUserNameAsync(userName, cancellationToken)
            : Task.FromResult<AdminUser?>(null);
}
