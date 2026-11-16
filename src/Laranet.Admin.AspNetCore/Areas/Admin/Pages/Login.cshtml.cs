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
    IAdminLoginRateLimiter rateLimiter) : PageModel
{
    [BindProperty]
    public string UserName { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrEmpty(Password))
        {
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
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        rateLimiter.Reset(key);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user!.UserName),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, "Admin"),
        };

        await HttpContext.SignInAsync(
            AdminAuthenticationDefaults.Scheme,
            new ClaimsPrincipal(new ClaimsIdentity(
                claims,
                AdminAuthenticationDefaults.Scheme)),
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true,
            });

        return LocalRedirect(ValidateReturnUrl(ReturnUrl));
    }

    private string ValidateReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : "/admin";
}
