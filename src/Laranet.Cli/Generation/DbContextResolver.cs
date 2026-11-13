using System.Text.RegularExpressions;

namespace Laranet.Cli.Generation;

/// <summary>
/// Resolves the <c>DbContext</c> type used by CRUD code.
/// </summary>
internal static partial class DbContextResolver
{
    /// <summary>
    /// Resolves an explicit context type or discovers one in the nearest project.
    /// </summary>
    /// <param name="outputDirectory">The directory where code will be generated.</param>
    /// <param name="explicitContext">An optional fully qualified context type.</param>
    /// <returns>The fully qualified context type.</returns>
    /// <exception cref="CliException">No context can be resolved.</exception>
    public static string Resolve(string outputDirectory, string? explicitContext)
    {
        if (!string.IsNullOrWhiteSpace(explicitContext))
        {
            Validate(explicitContext);

            return explicitContext.Trim();
        }

        var projectDirectory = FindProjectDirectory(outputDirectory);

        if (projectDirectory is null)
        {
            throw new CliException(
                "Could not find a .csproj file to locate a DbContext. Use --context to specify the context type.");
        }

        var contexts = Discover(projectDirectory)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(context => context, StringComparer.Ordinal)
            .ToArray();

        return contexts.Length switch
        {
            0 => throw new CliException(
                "Could not find a DbContext type in the nearest project. Use --context to specify the context type."),
            1 => contexts[0],
            _ => throw new CliException(
                $"Multiple DbContext types were found: {string.Join(", ", contexts)}. Use --context to choose one."),
        };
    }

    /// <summary>
    /// Validates a fully qualified <c>DbContext</c> type.
    /// </summary>
    /// <param name="value">The type to validate.</param>
    /// <exception cref="CliException">The value is not a valid type name.</exception>
    public static void Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CliException("A DbContext type is required.");
        }

        try
        {
            NamespaceResolver.Validate(value.Trim());
        }
        catch (CliException)
        {
            throw new CliException($"'{value}' is not a valid DbContext type.");
        }
    }

    private static DirectoryInfo? FindProjectDirectory(string outputDirectory)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(outputDirectory));

        while (directory is not null)
        {
            if (directory.Exists
                && directory.EnumerateFiles("*.csproj").Any())
            {
                return directory;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static IEnumerable<string> Discover(DirectoryInfo projectDirectory)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };

        foreach (var file in projectDirectory.EnumerateFiles("*.cs", options))
        {
            var relativePath = Path.GetRelativePath(
                projectDirectory.FullName,
                file.FullName);

            if (relativePath
                .Split(Path.DirectorySeparatorChar)
                .Any(segment => segment is "bin" or "obj"))
            {
                continue;
            }

            var content = File.ReadAllText(file.FullName);
            var classMatch = ContextRegex().Match(content);

            if (!classMatch.Success)
            {
                continue;
            }

            var namespaceMatch = NamespaceRegex().Match(content);
            var contextName = classMatch.Groups["name"].Value;

            yield return namespaceMatch.Success
                ? $"{namespaceMatch.Groups["value"].Value}.{contextName}"
                : contextName;
        }
    }

    [GeneratedRegex(@"\bnamespace\s+(?<value>[A-Za-z_][A-Za-z0-9_.]*)\s*[;{]")]
    private static partial Regex NamespaceRegex();

    [GeneratedRegex(
        @"\bclass\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?:\([^)]*\))?\s*:\s*(?:[A-Za-z_][A-Za-z0-9_]*\.)*DbContext\b",
        RegexOptions.Singleline)]
    private static partial Regex ContextRegex();
}
