using Microsoft.EntityFrameworkCore;

namespace Laranet.Admin.EntityFrameworkCore;

/// <summary>
/// EF Core context for standalone Admin authentication data.
/// </summary>
public sealed class AdminAuthenticationDbContext(
    DbContextOptions<AdminAuthenticationDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the Admin user records.
    /// </summary>
    public DbSet<AdminUserEntity> Users => Set<AdminUserEntity>();

    /// <summary>
    /// Gets the password reset tokens.
    /// </summary>
    public DbSet<AdminPasswordResetTokenEntity> PasswordResetTokens =>
        Set<AdminPasswordResetTokenEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUserEntity>(entity =>
        {
            entity.HasKey(user => user.UserName);
            entity.Property(user => user.UserName).HasMaxLength(256);
            entity.Property(user => user.NormalizedUserName).HasMaxLength(256);
            entity.HasIndex(user => user.NormalizedUserName).IsUnique();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.SecurityStamp).IsRequired();
        });
        modelBuilder.Entity<AdminPasswordResetTokenEntity>(entity =>
        {
            entity.HasKey(token => token.TokenHash);
            entity.Property(token => token.TokenHash).HasMaxLength(128);
            entity.Property(token => token.UserName).HasMaxLength(256);
        });
    }
}

/// <summary>
/// EF Core representation of a one-time password reset token.
/// </summary>
public sealed class AdminPasswordResetTokenEntity
{
    /// <summary>Gets or sets the token hash.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Gets or sets the target user name.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Gets or sets the expiration timestamp.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// EF Core representation of an Admin user.
/// </summary>
public sealed class AdminUserEntity
{
    /// <summary>Gets or sets the user name.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Gets or sets the normalized user name.</summary>
    public string NormalizedUserName { get; set; } = string.Empty;

    /// <summary>Gets or sets the password hash.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the user is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the session security stamp.</summary>
    public string SecurityStamp { get; set; } = string.Empty;

    /// <summary>Gets or sets whether MFA is enabled.</summary>
    public bool MfaEnabled { get; set; }

    /// <summary>Gets or sets the TOTP secret.</summary>
    public string? MfaSecret { get; set; }

    /// <summary>Gets or sets JSON containing hashed recovery codes.</summary>
    public string? MfaRecoveryCodeHashes { get; set; }
}
