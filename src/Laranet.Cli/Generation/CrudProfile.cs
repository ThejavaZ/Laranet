namespace Laranet.Cli.Generation;

/// <summary>
/// Identifies the level of structure used by <c>make:crud</c>.
/// </summary>
internal enum CrudProfile
{
    Simple,
    Standard,
    Clean,
    Modular,
}

/// <summary>
/// Parses CRUD profile names supplied on the command line.
/// </summary>
internal static class CrudProfileParser
{
    /// <summary>
    /// Gets the profile names accepted on the command line.
    /// </summary>
    public static IReadOnlyList<string> Names { get; } =
    [
        "simple",
        "standard",
        "clean",
        "modular",
    ];

    /// <summary>
    /// Parses a profile name.
    /// </summary>
    /// <param name="value">The profile name, or <see langword="null"/> for the default.</param>
    /// <returns>The parsed profile.</returns>
    /// <exception cref="CliException">The profile name is not known.</exception>
    public static CrudProfile Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return CrudProfile.Simple;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "simple" => CrudProfile.Simple,
            "standard" => CrudProfile.Standard,
            "clean" => CrudProfile.Clean,
            "modular" => CrudProfile.Modular,
            _ => throw new CliException(
                $"'{value}' is not a known profile. Use {string.Join(", ", Names)}."),
        };
    }

    /// <summary>
    /// Gets the command-line name of a profile.
    /// </summary>
    /// <param name="profile">The CRUD profile.</param>
    /// <returns>The lowercase profile name.</returns>
    public static string Name(CrudProfile profile)
    {
        return profile switch
        {
            CrudProfile.Simple => "simple",
            CrudProfile.Standard => "standard",
            CrudProfile.Clean => "clean",
            CrudProfile.Modular => "modular",
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null),
        };
    }
}
