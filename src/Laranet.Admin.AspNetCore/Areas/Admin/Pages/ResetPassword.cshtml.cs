using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages;

[AllowAnonymous]
public sealed class ResetPasswordModel(
    IAdminUserStore users,
    IAdminPasswordResetTokenStore tokens,
    IAdminPasswordHasher passwordHasher,
    IAdminAuditSink audit) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    [BindProperty]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userName = string.IsNullOrWhiteSpace(Token)
            ? null
            : await tokens.ConsumeAsync(Token, cancellationToken);
        var user = userName is null
            ? null
            : await users.FindByUserNameAsync(userName, cancellationToken);

        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This reset link is invalid or expired.");
        }

        if (NewPassword.Length < 12)
        {
            ModelState.AddModelError(
                nameof(NewPassword),
                "The new password must contain at least 12 characters.");
        }

        if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(
                nameof(ConfirmPassword),
                "The passwords do not match.");
        }

        if (!ModelState.IsValid || user is null)
        {
            await AdminAudit.WriteSafeAsync(
                audit,
                HttpContext,
                "password-reset",
                "failure",
                details: "Invalid or expired token",
                cancellationToken: cancellationToken);
            return Page();
        }

        await users.SaveAsync(
            user with
            {
                PasswordHash = passwordHasher.Hash(NewPassword),
                SecurityStamp = Guid.NewGuid().ToString("N"),
            },
            cancellationToken);
        await AdminAudit.WriteSafeAsync(
            audit,
            HttpContext,
            "password-reset",
            "success",
            details: user.UserName,
            cancellationToken: cancellationToken);
        TempData["AdminMessage"] = "Password reset. Sign in with the new password.";
        return RedirectToPage("/Login");
    }
}
