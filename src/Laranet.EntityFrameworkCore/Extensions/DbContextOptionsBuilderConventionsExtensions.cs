using Laranet.EntityFrameworkCore.Conventions;
using Microsoft.EntityFrameworkCore;

namespace Laranet.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides opt-in Entity Framework Core conventions for auditing and soft delete.
/// </summary>
public static class DbContextOptionsBuilderConventionsExtensions
{
    /// <summary>
    /// Registers the interceptor that maintains <see cref="IAuditable"/> timestamps.
    /// </summary>
    /// <param name="optionsBuilder">The options builder to configure.</param>
    /// <param name="timeProvider">
    /// The clock used to produce timestamps. Defaults to
    /// <see cref="TimeProvider.System"/>.
    /// </param>
    /// <returns>The same options builder so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="optionsBuilder"/> is <see langword="null"/>.
    /// </exception>
    public static DbContextOptionsBuilder AddLaranetAuditing(
        this DbContextOptionsBuilder optionsBuilder,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        optionsBuilder.AddInterceptors(
            new AuditingSaveChangesInterceptor(timeProvider ?? TimeProvider.System));

        return optionsBuilder;
    }

    /// <summary>
    /// Registers the interceptor that maintains <see cref="IAuditable"/> timestamps.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="optionsBuilder">The options builder to configure.</param>
    /// <param name="timeProvider">
    /// The clock used to produce timestamps. Defaults to
    /// <see cref="TimeProvider.System"/>.
    /// </param>
    /// <returns>The same options builder so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="optionsBuilder"/> is <see langword="null"/>.
    /// </exception>
    public static DbContextOptionsBuilder<TContext> AddLaranetAuditing<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        TimeProvider? timeProvider = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        ((DbContextOptionsBuilder)optionsBuilder).AddLaranetAuditing(timeProvider);

        return optionsBuilder;
    }

    /// <summary>
    /// Registers the interceptor that converts physical deletions of
    /// <see cref="ISoftDeletable"/> entities into soft-delete updates.
    /// </summary>
    /// <param name="optionsBuilder">The options builder to configure.</param>
    /// <param name="timeProvider">
    /// The clock used to produce the deletion timestamp. Defaults to
    /// <see cref="TimeProvider.System"/>.
    /// </param>
    /// <returns>The same options builder so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="optionsBuilder"/> is <see langword="null"/>.
    /// </exception>
    public static DbContextOptionsBuilder AddLaranetSoftDelete(
        this DbContextOptionsBuilder optionsBuilder,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        optionsBuilder.AddInterceptors(
            new SoftDeleteSaveChangesInterceptor(timeProvider ?? TimeProvider.System));

        return optionsBuilder;
    }

    /// <summary>
    /// Registers the interceptor that converts physical deletions of
    /// <see cref="ISoftDeletable"/> entities into soft-delete updates.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="optionsBuilder">The options builder to configure.</param>
    /// <param name="timeProvider">
    /// The clock used to produce the deletion timestamp. Defaults to
    /// <see cref="TimeProvider.System"/>.
    /// </param>
    /// <returns>The same options builder so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="optionsBuilder"/> is <see langword="null"/>.
    /// </exception>
    public static DbContextOptionsBuilder<TContext> AddLaranetSoftDelete<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        TimeProvider? timeProvider = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        ((DbContextOptionsBuilder)optionsBuilder).AddLaranetSoftDelete(timeProvider);

        return optionsBuilder;
    }
}
