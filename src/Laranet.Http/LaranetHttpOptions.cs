using System.Text.Json;

namespace Laranet.Http;

/// <summary>
/// Configures the default behavior of a <see cref="LaranetHttp"/> client registered
/// through dependency injection.
/// </summary>
public sealed class LaranetHttpOptions
{
    /// <summary>
    /// Gets or sets the base address used for relative request URLs.
    /// </summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// Gets or sets the timeout applied to each HTTP attempt. When
    /// <see langword="null"/>, the <see cref="HttpClient.Timeout"/> default applies.
    /// Use <see cref="RequestOptions.Timeout"/> for a whole-operation timeout that
    /// includes retry delays.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the default JSON options. The default uses
    /// <see cref="JsonSerializerDefaults.Web"/>.
    /// </summary>
    public JsonSerializerOptions JsonOptions { get; set; } =
        new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Gets or sets the default retry policy. The default retries transient failures
    /// for idempotent requests only.
    /// </summary>
    public RetryOptions Retry { get; set; } = new();
}
