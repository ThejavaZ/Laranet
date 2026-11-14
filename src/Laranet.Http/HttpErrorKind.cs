namespace Laranet.Http;

/// <summary>
/// Classifies why an HTTP operation did not produce a successful result.
/// </summary>
public enum HttpErrorKind
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    None = 0,

    /// <summary>
    /// The server returned a non-success HTTP status code.
    /// </summary>
    Http,

    /// <summary>
    /// The request could not be sent because of a network failure.
    /// </summary>
    Network,

    /// <summary>
    /// The request exceeded its timeout.
    /// </summary>
    Timeout,

    /// <summary>
    /// The response body could not be deserialized into the requested type.
    /// </summary>
    Deserialization,

    /// <summary>
    /// The response did not match the expected contract, such as a successful
    /// response with no body when a body was expected.
    /// </summary>
    UnexpectedResponse,
}
