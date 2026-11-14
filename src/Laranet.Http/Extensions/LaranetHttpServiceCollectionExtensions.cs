using Laranet.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides dependency injection registration for <see cref="LaranetHttp"/>.
/// </summary>
public static class LaranetHttpServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="LaranetHttp"/> as a typed <see cref="HttpClient"/> client
    /// and applies the configured defaults.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">An optional delegate to configure the client defaults.</param>
    /// <returns>
    /// The native <see cref="IHttpClientBuilder"/> so additional handlers and the
    /// primary handler can be configured.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <see langword="null"/>.
    /// </exception>
    public static IHttpClientBuilder AddLaranetHttp(
        this IServiceCollection services,
        Action<LaranetHttpOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<LaranetHttpOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services.AddHttpClient<LaranetHttp>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<LaranetHttpOptions>>()
                .Value;

            if (options.BaseAddress is not null)
            {
                if (!options.BaseAddress.IsAbsoluteUri)
                {
                    throw new InvalidOperationException(
                        "LaranetHttpOptions.BaseAddress must be an absolute URI.");
                }

                client.BaseAddress = options.BaseAddress;
            }

            if (options.Timeout is { } timeout)
            {
                if (timeout <= TimeSpan.Zero)
                {
                    throw new InvalidOperationException(
                        "LaranetHttpOptions.Timeout must be greater than zero.");
                }

                client.Timeout = timeout;
            }
        });
    }
}
