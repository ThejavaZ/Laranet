using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Laranet.Admin.AspNetCore;

/// <summary>
/// Identifies the authentication scheme used by the standalone Admin login.
/// </summary>
public static class AdminAuthenticationDefaults
{
    /// <summary>
    /// Gets the default Admin cookie scheme.
    /// </summary>
    public const string Scheme = "Laranet.Admin";
}

/// <summary>
/// Represents a user that can sign in to the standalone Admin authentication flow.
/// </summary>
public sealed record AdminUser(
    string UserName,
    string PasswordHash,
    bool IsActive = true,
    string? SecurityStamp = null,
    bool MfaEnabled = false,
    string? MfaSecret = null);

/// <summary>
/// Stores one-time password reset tokens.
/// </summary>
public interface IAdminPasswordResetTokenStore
{
    /// <summary>
    /// Creates a single-use token for a user.
    /// </summary>
    Task<string> CreateAsync(
        string userName,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes a valid token and returns its user.
    /// </summary>
    Task<string?> ConsumeAsync(
        string token,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Delivers password reset links through an application-provided channel.
/// </summary>
public interface IAdminPasswordResetSender
{
    /// <summary>
    /// Sends a password reset link.
    /// </summary>
    Task SendAsync(
        string userName,
        string resetUrl,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides time-based one-time password operations for Admin MFA.
/// </summary>
public interface IAdminMfaProvider
{
    /// <summary>
    /// Creates a new Base32 secret.
    /// </summary>
    string CreateSecret();

    /// <summary>
    /// Creates an authenticator provisioning URI.
    /// </summary>
    string CreateProvisioningUri(string userName, string secret);

    /// <summary>
    /// Validates a six-digit TOTP code.
    /// </summary>
    bool ValidateCode(string secret, string code);
}

/// <summary>
/// Contains claims used by the standalone Admin authentication cookie.
/// </summary>
public static class AdminAuthenticationClaimTypes
{
    /// <summary>
    /// Gets the claim containing the user's session security stamp.
    /// </summary>
    public const string SecurityStamp = "laranet_admin_security_stamp";
}

/// <summary>
/// Looks up users for the standalone Admin login.
/// </summary>
public interface IAdminUserStore
{
    /// <summary>
    /// Finds a user by normalized user name.
    /// </summary>
    Task<AdminUser?> FindByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a user.
    /// </summary>
    Task SaveAsync(
        AdminUser user,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Hashes and verifies Admin passwords.
/// </summary>
public interface IAdminPasswordHasher
{
    /// <summary>
    /// Creates a password hash.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Verifies a password against a stored hash.
    /// </summary>
    bool Verify(string password, string passwordHash);
}

/// <summary>
/// Configures the standalone Admin authentication flow.
/// </summary>
public sealed class AdminAuthenticationOptions
{
    private readonly List<(string UserName, string Password)> users = [];

    /// <summary>
    /// Gets or sets the cookie name.
    /// </summary>
    public string CookieName { get; set; } = "Laranet.Admin";

    /// <summary>
    /// Gets or sets the login path.
    /// </summary>
    public string LoginPath { get; set; } = "/admin/login";

    /// <summary>
    /// Gets or sets the cookie lifetime.
    /// </summary>
    public TimeSpan CookieLifetime { get; set; } = TimeSpan.FromHours(8);

    /// <summary>
    /// Gets or sets the cookie SameSite policy.
    /// </summary>
    public SameSiteMode CookieSameSite { get; set; } = SameSiteMode.Lax;

    /// <summary>
    /// Gets or sets the cookie secure policy.
    /// </summary>
    public CookieSecurePolicy CookieSecurePolicy { get; set; } =
        CookieSecurePolicy.SameAsRequest;

    /// <summary>
    /// Gets or sets the number of failed attempts allowed in the lockout window.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the temporary lockout duration.
    /// </summary>
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the password reset token lifetime.
    /// </summary>
    public TimeSpan PasswordResetTokenLifetime { get; set; } = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Adds an in-memory user intended for development and small demonstrations.
    /// Production applications should replace the store with persistent storage.
    /// </summary>
    public void AddUser(string userName, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        if (password.Length < 12)
        {
            throw new ArgumentException(
                "Admin passwords must contain at least 12 characters.",
                nameof(password));
        }

        users.Add((userName.Trim(), password));
    }

    internal IReadOnlyList<(string UserName, string Password)> Users => users;
}

/// <summary>
/// Adds standalone cookie authentication for the Admin pages.
/// </summary>
public static class AdminAuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the standalone Admin login, cookie, password hasher, and in-memory
    /// user store.
    /// </summary>
    public static IServiceCollection AddLaranetAdminAuthentication(
        this IServiceCollection services,
        Action<AdminAuthenticationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new AdminAuthenticationOptions();
        configure?.Invoke(options);

        if (string.IsNullOrWhiteSpace(options.CookieName))
        {
            throw new ArgumentException("The Admin cookie name is required.", nameof(configure));
        }

        if (options.CookieLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                "The Admin cookie lifetime must be positive.",
                nameof(configure));
        }

        if (options.MaxFailedAttempts <= 0 || options.LockoutDuration <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Login rate limiting values must be positive.",
                nameof(configure));
        }

        if (options.PasswordResetTokenLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                "The password reset token lifetime must be positive.",
                nameof(configure));
        }

        var hasher = new Pbkdf2AdminPasswordHasher();
        var users = options.Users
            .Select(user => new AdminUser(
                user.UserName,
                hasher.Hash(user.Password),
                SecurityStamp: Guid.NewGuid().ToString("N")))
            .ToArray();

        services.AddSingleton(options);
        services.TryAddSingleton<IAdminPasswordHasher>(hasher);
        services.TryAddSingleton<IAdminUserStore>(
            new InMemoryAdminUserStore(users));
        services.TryAddSingleton<IAdminLoginRateLimiter>(
            new InMemoryAdminLoginRateLimiter(
                options.MaxFailedAttempts,
                options.LockoutDuration));
        services.TryAddSingleton<IAdminPasswordResetTokenStore, InMemoryAdminPasswordResetTokenStore>();
        services.TryAddSingleton<IAdminPasswordResetSender, NullAdminPasswordResetSender>();
        services.TryAddSingleton<IAdminMfaProvider, TotpAdminMfaProvider>();
        services.AddScoped<AdminCookieEvents>();
        services.AddAuthentication(authentication =>
            {
                authentication.DefaultAuthenticateScheme = AdminAuthenticationDefaults.Scheme;
                authentication.DefaultChallengeScheme = AdminAuthenticationDefaults.Scheme;
                authentication.DefaultSignInScheme = AdminAuthenticationDefaults.Scheme;
            })
            .AddCookie(AdminAuthenticationDefaults.Scheme, cookie =>
            {
                cookie.Cookie.Name = options.CookieName;
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.SameSite = options.CookieSameSite;
                cookie.Cookie.SecurePolicy = options.CookieSecurePolicy;
                cookie.ExpireTimeSpan = options.CookieLifetime;
                cookie.SlidingExpiration = true;
                cookie.LoginPath = options.LoginPath;
                cookie.ReturnUrlParameter = "returnUrl";
                cookie.EventsType = typeof(AdminCookieEvents);
            });

        services.AddAuthorization();
        return services;
    }
}

internal sealed class AdminCookieEvents(IAdminUserStore users)
    : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userName = context.Principal?.Identity?.Name;
        var stamp = context.Principal?
            .FindFirst(AdminAuthenticationClaimTypes.SecurityStamp)?
            .Value;

        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(stamp))
        {
            await RejectAsync(context);
            return;
        }

        var user = await users.FindByUserNameAsync(userName, context.HttpContext.RequestAborted);
        if (user is null ||
            !user.IsActive ||
            !string.Equals(user.SecurityStamp, stamp, StringComparison.Ordinal))
        {
            await RejectAsync(context);
        }
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.HttpContext.Items["Laranet.Admin.SessionExpired"] = true;
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(AdminAuthenticationDefaults.Scheme);
    }

    public override Task RedirectToLogin(
        RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Path.StartsWithSegments("/admin") &&
            (context.HttpContext.Items.ContainsKey("Laranet.Admin.SessionExpired") ||
             context.Request.Cookies.ContainsKey(context.Options.Cookie.Name!)))
        {
            context.RedirectUri = QueryHelpers.AddQueryString(
                context.RedirectUri,
                "reason",
                "expired");
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
}

internal static class AdminSignIn
{
    public static Task SignInAsync(HttpContext context, AdminUser user) =>
        context.SignInAsync(
            AdminAuthenticationDefaults.Scheme,
            new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.UserName),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(
                    AdminAuthenticationClaimTypes.SecurityStamp,
                    user.SecurityStamp!),
            ],
            AdminAuthenticationDefaults.Scheme)),
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true,
            });
}

/// <summary>
/// Limits repeated failed Admin login attempts.
/// </summary>
public interface IAdminLoginRateLimiter
{
    /// <summary>
    /// Determines whether a login attempt is currently allowed.
    /// </summary>
    bool IsAllowed(string key);

    /// <summary>
    /// Records a failed login attempt.
    /// </summary>
    void RegisterFailure(string key);

    /// <summary>
    /// Clears failures after a successful login.
    /// </summary>
    void Reset(string key);
}

internal sealed class InMemoryAdminLoginRateLimiter(
    int maxFailedAttempts,
    TimeSpan lockoutDuration) : IAdminLoginRateLimiter
{
    private readonly object sync = new();
    private readonly Dictionary<string, FailureState> failures =
        new(StringComparer.Ordinal);

    public bool IsAllowed(string key)
    {
        lock (sync)
        {
            if (!failures.TryGetValue(key, out var state))
            {
                return true;
            }

            if (state.LockedUntil <= DateTimeOffset.UtcNow)
            {
                failures.Remove(key);
                return true;
            }

            return state.Count < maxFailedAttempts;
        }
    }

    public void RegisterFailure(string key)
    {
        lock (sync)
        {
            failures.TryGetValue(key, out var state);
            failures[key] = new FailureState(
                state.Count + 1,
                DateTimeOffset.UtcNow.Add(lockoutDuration));
        }
    }

    public void Reset(string key)
    {
        lock (sync)
        {
            failures.Remove(key);
        }
    }

    private readonly record struct FailureState(
        int Count,
        DateTimeOffset LockedUntil);
}

internal sealed class InMemoryAdminUserStore(IReadOnlyList<AdminUser> users)
    : IAdminUserStore
{
    private readonly object sync = new();
    private readonly Dictionary<string, AdminUser> entries =
        users.ToDictionary(user => user.UserName, StringComparer.OrdinalIgnoreCase);

    public Task<AdminUser?> FindByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        AdminUser? user;
        lock (sync)
        {
            entries.TryGetValue(userName.Trim(), out user);
        }

        return Task.FromResult(user);
    }

    public Task SaveAsync(
        AdminUser user,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            entries[user.UserName] = user;
        }
        return Task.CompletedTask;
    }
}

internal sealed class Pbkdf2AdminPasswordHasher : IAdminPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 600_000;
    private const string Algorithm = "pbkdf2-sha256";

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return string.Join(
            '$',
            Algorithm,
            Iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(passwordHash);

        try
        {
            var parts = passwordHash.Split('$');
            if (parts.Length != 4 ||
                !string.Equals(parts[0], Algorithm, StringComparison.Ordinal) ||
                !int.TryParse(parts[1], out var iterations) ||
                iterations <= 0)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

internal sealed class InMemoryAdminPasswordResetTokenStore
    : IAdminPasswordResetTokenStore
{
    private readonly object sync = new();
    private readonly Dictionary<string, ResetToken> tokens = new(StringComparer.Ordinal);

    public Task<string> CreateAsync(
        string userName,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        lock (sync)
        {
            tokens[hash] = new ResetToken(userName, expiresAt);
        }

        return Task.FromResult(token);
    }

    public Task<string?> ConsumeAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        lock (sync)
        {
            if (!tokens.Remove(hash, out var resetToken) ||
                resetToken.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return Task.FromResult<string?>(null);
            }

            return Task.FromResult<string?>(resetToken.UserName);
        }
    }

    private readonly record struct ResetToken(
        string UserName,
        DateTimeOffset ExpiresAt);
}

internal sealed class NullAdminPasswordResetSender : IAdminPasswordResetSender
{
    public Task SendAsync(
        string userName,
        string resetUrl,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class TotpAdminMfaProvider : IAdminMfaProvider
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int StepSeconds = 30;

    public string CreateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        var result = new StringBuilder(32);
        var buffer = 0;
        var bits = 0;

        foreach (var value in bytes)
        {
            buffer = (buffer << 8) | value;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                result.Append(Alphabet[(buffer >> bits) & 31]);
            }
        }

        if (bits > 0)
        {
            result.Append(Alphabet[(buffer << (5 - bits)) & 31]);
        }

        return result.ToString();
    }

    public string CreateProvisioningUri(string userName, string secret) =>
        $"otpauth://totp/Laranet%20Admin:{Uri.EscapeDataString(userName)}" +
        $"?secret={secret}&issuer=Laranet%20Admin&algorithm=SHA1&digits=6&period=30";

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) ||
            !int.TryParse(code, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ||
            value is < 0 or > 999999)
        {
            return false;
        }

        var normalized = code.PadLeft(6, '0');
        var counter = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / StepSeconds);
        for (var offset = -1; offset <= 1; offset++)
        {
            if (ComputeCode(secret, (ulong)((long)counter + offset)) == normalized)
            {
                return true;
            }
        }

        return false;
    }

    private static string ComputeCode(string secret, ulong counter)
    {
        var key = DecodeBase32(secret);
        var data = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(data);
        }

        using var hmac = new System.Security.Cryptography.HMACSHA1(key);
        var hash = hmac.ComputeHash(data);
        var index = hash[^1] & 0x0f;
        var binary = ((hash[index] & 0x7f) << 24)
            | (hash[index + 1] << 16)
            | (hash[index + 2] << 8)
            | hash[index + 3];
        return (binary % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private static byte[] DecodeBase32(string value)
    {
        var buffer = new List<byte>();
        var bits = 0;
        var current = 0;
        foreach (var character in value.ToUpperInvariant())
        {
            var index = Alphabet.IndexOf(character);
            if (index < 0)
            {
                continue;
            }

            current = (current << 5) | index;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                buffer.Add((byte)(current >> bits));
                current &= (1 << bits) - 1;
            }
        }

        return buffer.ToArray();
    }
}
