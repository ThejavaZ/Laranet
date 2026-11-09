namespace Laranet.Cli;

/// <summary>
/// Represents an expected command-line failure with a user-facing message.
/// </summary>
internal sealed class CliException(string message) : Exception(message);
