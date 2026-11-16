using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages;

[AllowAnonymous]
public sealed class LoginModel(
    IAdminUserStore users,
    IAdminPasswordHasher passwordHasher,
    IAdminLoginRateLimiter rateLimiter,
    IAdminAuditSink audit) : PageModel
{
    [BindProperty]
    public string UserName { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Reason { get; set; }

    public bool SessionExpired =>
        string.Equals(Reason, "expired", StringComparison.OrdinalIgnoreCase);

    public void OnGet()
    {
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrEmpty(Password))
        {
            await AdminAudit.WriteSafeAsync(
                audit,
                HttpContext,
                "login",
                "failure",
                details: "Missing credentials",
                cancellationToken: cancellationToken);
            ModelState.AddModelError(string.Empty, "Enter your username and password.");
            return Page();
        }

        var key = $"{HttpContext.Connection.RemoteIpAddress}|{UserName.Trim()}";
        var user = await users.FindByUserNameAsync(UserName, cancellationToken);
        var valid = rateLimiter.IsAllowed(key) &&
            user is not null &&
            user.IsActive &&
            passwordHasher.Verify(Password, user.PasswordHash);

        if (!valid)
        {
            rateLimiter.RegisterFailure(key);
            await AdminAudit.WriteSafeAsync(
                audit,
                HttpContext,
                "login",
                "failure",
                details: "Invalid credentials or locked account",
                cancellationToken: cancellationToken);
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        rateLimiter.Reset(key);
        var authenticatedUser = user!;
        if (string.IsNullOrWhiteSpace(authenticatedUser.SecurityStamp))
        {
            authenticatedUser = authenticatedUser with
            {
                SecurityStamp = Guid.NewGuid().ToString("N"),
            };
            await users.SaveAsync(authenticatedUser, cancellationToken);
        }

        if (authenticatedUser.MfaEnabled &&
            !string.IsNullOrWhiteSpace(authenticatedUser.MfaSecret))
        {
            TempData["AdminMfaUser"] = authenticatedUser.UserName;
            TempData["AdminMfaReturnUrl"] = ValidateReturnUrl(ReturnUrl);
            return RedirectToPage("/Mfa");
        }

        await AdminSignIn.SignInAsync(HttpContext, authenticatedUser);

        await AdminAudit.WriteSafeAsync(
            audit,
            HttpContext,
            "login",
            "success",
            cancellationToken: cancellationToken);

        return LocalRedirect(ValidateReturnUrl(ReturnUrl));
    }

    private string ValidateReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : "/admin";
}
