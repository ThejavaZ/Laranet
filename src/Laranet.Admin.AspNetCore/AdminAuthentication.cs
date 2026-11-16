using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
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
    string? SecurityStamp = null);

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
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(AdminAuthenticationDefaults.Scheme);
    }
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
