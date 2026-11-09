namespace Laranet.Cli.Generation;

/// <summary>
/// Parses project layout names supplied on the command line.
/// </summary>
internal static class ProjectLayoutParser
{
    /// <summary>
    /// Parses a layout name.
    /// </summary>
    /// <param name="value">The layout name, or <see langword="null"/> for the default.</param>
    /// <returns>The parsed layout.</returns>
    /// <exception cref="CliException">The layout name is not known.</exception>
    public static ProjectLayout Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ProjectLayout.Project;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "project" => ProjectLayout.Project,
            "repository" or "repo" => ProjectLayout.Repository,
            _ => throw new CliException(
                $"'{value}' is not a known layout. Use 'project' or 'repository'."),
        };
    }
}
