namespace Laranet.Cli.Generation;

/// <summary>
/// Validates project names and derives their namespace.
/// </summary>
internal static class ProjectName
{
    /// <summary>
    /// Validates the name used as a project folder and project file name.
    /// </summary>
    /// <param name="value">The raw command-line value.</param>
    /// <returns>The trimmed name.</returns>
    /// <exception cref="CliException">The name cannot be used as a folder name.</exception>
    public static string Folder(string value)
    {
        var trimmed = value.Trim();

        if (trimmed.Length == 0)
        {
            throw new CliException("A project name is required.");
        }

        if (trimmed is "." or ".."
            || trimmed.Contains('/', StringComparison.Ordinal)
            || trimmed.Contains('\\', StringComparison.Ordinal)
            || trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new CliException(
                $"'{value}' is not a valid project name. Use a simple folder name without path separators.");
        }

        return trimmed;
    }

    /// <summary>
    /// Derives a valid root namespace from a project name.
    /// </summary>
    /// <param name="folderName">A validated project folder name.</param>
    /// <returns>A PascalCase namespace.</returns>
    /// <exception cref="CliException">The name cannot be converted into a valid identifier.</exception>
    public static string Namespace(string folderName)
    {
        return NameNormalizer.Normalize(folderName);
    }
}
