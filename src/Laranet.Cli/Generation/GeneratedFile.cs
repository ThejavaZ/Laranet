namespace Laranet.Cli.Generation;

/// <summary>
/// Represents a file to be generated.
/// </summary>
/// <param name="Path">The absolute destination path.</param>
/// <param name="Content">The file content.</param>
internal sealed record GeneratedFile(string Path, string Content);
