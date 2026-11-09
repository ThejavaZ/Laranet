using System.Text.RegularExpressions;

namespace Laranet.Cli.Generation;

/// <summary>
/// Normalizes user-provided names into valid C# type names.
/// </summary>
internal static partial class NameNormalizer
{
    /// <summary>
    /// Converts a user-provided name into a PascalCase C# identifier.
    /// </summary>
    /// <param name="value">The raw command-line value.</param>
    /// <returns>A normalized identifier.</returns>
    /// <exception cref="CliException">The value cannot be converted into a valid identifier.</exception>
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CliException("A name is required.");
        }

        var trimmed = value.Trim();

        if (trimmed.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^3];
        }

        var segments = SeparatorRegex()
            .Split(trimmed)
            .Where(segment => segment.Length > 0)
            .ToArray();

        if (segments.Length == 0)
        {
            throw InvalidName(value);
        }

        var normalized = string.Concat(
            segments.Select(segment =>
                char.ToUpperInvariant(segment[0]) + segment[1..]));

        if (!IdentifierRegex().IsMatch(normalized))
        {
            throw InvalidName(value);
        }

        return normalized;
    }

    /// <summary>
    /// Appends a suffix when the name does not already end with it.
    /// </summary>
    /// <param name="name">A normalized type name.</param>
    /// <param name="suffix">The suffix to ensure.</param>
    /// <returns>The name with the suffix applied.</returns>
    public static string EnsureSuffix(string name, string suffix)
    {
        return name.EndsWith(suffix, StringComparison.Ordinal)
            ? name
            : name + suffix;
    }

    private static CliException InvalidName(string value)
    {
        return new CliException(
            $"'{value}' is not a valid name. Use letters, digits, spaces, hyphens, or underscores, and do not start with a digit.");
    }

    [GeneratedRegex("[^A-Za-z0-9]+")]
    private static partial Regex SeparatorRegex();

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex IdentifierRegex();
}
