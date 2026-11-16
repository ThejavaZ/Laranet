using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Laranet.Admin.AspNetCore.Areas.Admin.Pages;

[AllowAnonymous]
public sealed class ForgotPasswordModel(
    IAdminUserStore users,
    IAdminPasswordResetTokenStore tokens,
    IAdminPasswordResetSender sender,
    AdminAuthenticationOptions options,
    IAdminLoginRateLimiter rateLimiter,
    IAdminAuditSink audit) : PageModel
{
    [BindProperty]
    public string UserName { get; set; } = string.Empty;

    public bool Sent { get; private set; }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var key = $"reset|{HttpContext.Connection.RemoteIpAddress}|{UserName.Trim()}";
        if (rateLimiter.IsAllowed(key) &&
            !string.IsNullOrWhiteSpace(UserName))
        {
            var user = await users.FindByUserNameAsync(UserName, cancellationToken);
            if (user is not null && user.IsActive)
            {
                var token = await tokens.CreateAsync(
                    user.UserName,
                    DateTimeOffset.UtcNow.Add(options.PasswordResetTokenLifetime),
                    cancellationToken);
                var resetUrl = Url.Page(
                    "/ResetPassword",
                    pageHandler: null,
                    values: new { token },
                    protocol: Request.Scheme,
                    host: Request.Host.Value);
                await sender.SendAsync(user.UserName, resetUrl!, cancellationToken);
            }
        }
        rateLimiter.RegisterFailure(key);

        await AdminAudit.WriteSafeAsync(
            audit,
            HttpContext,
            "password-reset-request",
            "accepted",
            details: "Response intentionally does not disclose account existence",
            cancellationToken: cancellationToken);
        Sent = true;
        return Page();
    }
}
