using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages;

[Authorize(AuthenticationSchemes = AdminAuthenticationDefaults.Scheme)]
public sealed class PasswordModel(
    IAdminUserStore users,
    IAdminPasswordHasher passwordHasher) : PageModel
{
    [BindProperty]
    public string CurrentPassword { get; set; } = string.Empty;

    [BindProperty]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userName = User.Identity?.Name;
        var user = string.IsNullOrWhiteSpace(userName)
            ? null
            : await users.FindByUserNameAsync(userName, cancellationToken);

        if (user is null ||
            !user.IsActive ||
            !passwordHasher.Verify(CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError(
                nameof(CurrentPassword),
                "The current password is incorrect.");
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
            return Page();
        }

        await users.SaveAsync(
            user with
            {
                PasswordHash = passwordHasher.Hash(NewPassword),
                SecurityStamp = Guid.NewGuid().ToString("N"),
            },
            cancellationToken);

        await HttpContext.SignOutAsync(AdminAuthenticationDefaults.Scheme);
        TempData["AdminMessage"] = "Password changed. Sign in again.";
        return RedirectToPage("/Login");
    }
}
