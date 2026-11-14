using System.Text.Json;

namespace Laranet.Http;

/// <summary>
/// Groups the per-request configuration for a <see cref="LaranetHttp"/> operation.
/// </summary>
public sealed record RequestOptions
{
    /// <summary>
    /// Gets the request headers to add to the <see cref="HttpRequestMessage"/>.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// Gets the content headers to add to the request body. Content headers require a
    /// request body; setting this property on a bodyless operation is an error.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ContentHeaders { get; init; }

    /// <summary>
    /// Gets the timeout for the whole operation, including reading the response. When
    /// <see langword="null"/>, the <see cref="HttpClient.Timeout"/> applies.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Gets the JSON options used to serialize the request and deserialize the
    /// response. When <see langword="null"/>, the options configured for the client
    /// apply.
    /// </summary>
    public JsonSerializerOptions? JsonOptions { get; init; }

    /// <summary>
    /// Gets a value indicating whether a failed result should be thrown as a native
    /// .NET exception instead of returned.
    /// </summary>
    public bool ThrowOnError { get; init; }
}
