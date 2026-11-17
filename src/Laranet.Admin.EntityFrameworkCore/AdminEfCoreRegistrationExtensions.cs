using Laranet.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Laranet.Admin.EntityFrameworkCore;

/// <summary>
/// EF Core registration sugar for <c>AddLaranetAdmin</c> options.
/// </summary>
public static class AdminEfCoreRegistrationExtensions
{
    /// <summary>
    /// Registers <see cref="EfCoreAdminDataAdapter"/> for <typeparamref name="TContext"/>
    /// as the admin data adapter.
    /// </summary>
    /// <typeparam name="TContext">The application's DbContext.</typeparam>
    /// <param name="options">The registration options being configured.</param>
    /// <returns>The same options instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    public static AdminRegistrationOptions UseEfCore<TContext>(
        this AdminRegistrationOptions options)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.AddAdapter(services =>
            new EfCoreAdminDataAdapter(services.GetRequiredService<TContext>()));
    }
}
