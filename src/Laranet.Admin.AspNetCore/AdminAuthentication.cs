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
    bool IsActive = true);

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

        var hasher = new Pbkdf2AdminPasswordHasher();
        var users = options.Users
            .Select(user => new AdminUser(user.UserName, hasher.Hash(user.Password)))
            .ToArray();

        services.AddSingleton(options);
        services.TryAddSingleton<IAdminPasswordHasher>(hasher);
        services.TryAddSingleton<IAdminUserStore>(
            new InMemoryAdminUserStore(users));
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
            });

        services.AddAuthorization();
        return services;
    }
}

internal sealed class InMemoryAdminUserStore(IReadOnlyList<AdminUser> users)
    : IAdminUserStore
{
    public Task<AdminUser?> FindByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        var user = users.FirstOrDefault(item =>
            string.Equals(item.UserName, userName.Trim(), StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(user);
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
