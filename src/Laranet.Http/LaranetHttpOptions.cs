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
    /// Gets or sets the default timeout for requests. When <see langword="null"/>, the
    /// <see cref="HttpClient.Timeout"/> default applies.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the default JSON options. The default uses
    /// <see cref="JsonSerializerDefaults.Web"/>.
    /// </summary>
    public JsonSerializerOptions JsonOptions { get; set; } =
        new(JsonSerializerDefaults.Web);
}
