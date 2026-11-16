using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Laranet.Admin.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Laranet.Admin.EntityFrameworkCore;

/// <summary>
/// Stores standalone Admin users in an application-provided EF Core context.
/// </summary>
public sealed class EfCoreAdminUserStore(
    AdminAuthenticationDbContext db) : IAdminUserStore
{
    /// <inheritdoc />
    public async Task<AdminUser?> FindByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(userName);
        var entity = await db.Users
            .SingleOrDefaultAsync(
                user => user.NormalizedUserName == normalized,
                cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        AdminUser user,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(user.UserName);
        var entity = await db.Users
            .SingleOrDefaultAsync(
                item => item.NormalizedUserName == normalized,
                cancellationToken);

        if (entity is null)
        {
            entity = new AdminUserEntity
            {
                UserName = user.UserName.Trim(),
                NormalizedUserName = normalized,
            };
            db.Users.Add(entity);
        }

        entity.PasswordHash = user.PasswordHash;
        entity.IsActive = user.IsActive;
        entity.SecurityStamp = user.SecurityStamp
            ?? Guid.NewGuid().ToString("N");
        entity.MfaEnabled = user.MfaEnabled;
        entity.MfaSecret = user.MfaSecret;
        entity.MfaRecoveryCodeHashes = user.MfaRecoveryCodeHashes is null
            ? null
            : JsonSerializer.Serialize(user.MfaRecoveryCodeHashes);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static AdminUser ToModel(AdminUserEntity entity) =>
        new(
            entity.UserName,
            entity.PasswordHash,
            entity.IsActive,
            entity.SecurityStamp,
            entity.MfaEnabled,
            entity.MfaSecret,
            entity.MfaRecoveryCodeHashes is null
                ? null
                : JsonSerializer.Deserialize<string[]>(
                    entity.MfaRecoveryCodeHashes));

    private static string Normalize(string userName) =>
        userName.Trim().ToUpperInvariant();
}

/// <summary>
/// Stores password reset tokens in EF Core.
/// </summary>
public sealed class EfCoreAdminPasswordResetTokenStore(
    AdminAuthenticationDbContext db) : IAdminPasswordResetTokenStore
{
    /// <inheritdoc />
    public async Task<string> CreateAsync(
        string userName,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        db.PasswordResetTokens.Add(new AdminPasswordResetTokenEntity
        {
            TokenHash = Hash(token),
            UserName = userName,
            ExpiresAt = expiresAt,
        });
        await db.SaveChangesAsync(cancellationToken);
        return token;
    }

    /// <inheritdoc />
    public async Task<string?> ConsumeAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.PasswordResetTokens
            .SingleOrDefaultAsync(item => item.TokenHash == Hash(token), cancellationToken);
        if (entity is null || entity.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        db.PasswordResetTokens.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity.UserName;
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

/// <summary>
/// Registers the EF Core store for standalone Admin authentication.
/// </summary>
public static class AdminEntityFrameworkCoreAuthenticationExtensions
{
    /// <summary>
    /// Uses <see cref="AdminAuthenticationDbContext"/> for Admin users.
    /// Register the context separately with <c>AddDbContext</c>.
    /// </summary>
    public static IServiceCollection AddLaranetAdminEntityFrameworkCoreAuthentication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IAdminUserStore, EfCoreAdminUserStore>();
        services.AddScoped<IAdminPasswordResetTokenStore, EfCoreAdminPasswordResetTokenStore>();
        return services;
    }
}
