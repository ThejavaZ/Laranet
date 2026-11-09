using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Laranet.Cli.Generation;

/// <summary>
/// Resolves the namespace used by generated code.
/// </summary>
internal static partial class NamespaceResolver
{
    /// <summary>
    /// Resolves an explicit namespace or infers one from the nearest project file.
    /// </summary>
    /// <param name="outputDirectory">The directory where files will be generated.</param>
    /// <param name="explicitNamespace">An optional namespace provided by the user.</param>
    /// <returns>A valid namespace.</returns>
    /// <exception cref="CliException">No namespace can be inferred or the value is invalid.</exception>
    public static string Resolve(string outputDirectory, string? explicitNamespace)
    {
        if (!string.IsNullOrWhiteSpace(explicitNamespace))
        {
            Validate(explicitNamespace.Trim());

            return explicitNamespace.Trim();
        }

        var projectFile = FindProjectFile(outputDirectory);

        if (projectFile is null)
        {
            throw new CliException(
                "Could not infer a namespace because no .csproj file was found. Use --namespace to specify one.");
        }

        var projectNamespace = ReadProjectNamespace(projectFile);
        var sanitized = Sanitize(projectNamespace);

        Validate(sanitized);

        return sanitized;
    }

    /// <summary>
    /// Validates a dotted C# namespace.
    /// </summary>
    /// <param name="value">The namespace to validate.</param>
    /// <exception cref="CliException">The namespace is not valid.</exception>
    public static void Validate(string value)
    {
        var segments = value.Split('.');

        if (segments.Any(segment => !IdentifierRegex().IsMatch(segment)))
        {
            throw new CliException($"'{value}' is not a valid namespace.");
        }
    }

    private static FileInfo? FindProjectFile(string outputDirectory)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(outputDirectory));

        while (directory is not null)
        {
            if (directory.Exists)
            {
                var projectFile = directory
                    .EnumerateFiles("*.csproj")
                    .OrderBy(file => file.Name, StringComparer.Ordinal)
                    .FirstOrDefault();

                if (projectFile is not null)
                {
                    return projectFile;
                }
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string ReadProjectNamespace(FileInfo projectFile)
    {
        try
        {
            var document = XDocument.Load(projectFile.FullName);
            var rootNamespace = document
                .Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "RootNamespace")
                ?.Value;

            if (!string.IsNullOrWhiteSpace(rootNamespace))
            {
                return rootNamespace.Trim();
            }

            var assemblyName = document
                .Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "AssemblyName")
                ?.Value;

            if (!string.IsNullOrWhiteSpace(assemblyName))
            {
                return assemblyName.Trim();
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            // Fall back to the project file name when the project cannot be read.
        }

        return Path.GetFileNameWithoutExtension(projectFile.Name);
    }

    private static string Sanitize(string value)
    {
        var segments = value.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var builder = new StringBuilder();

        foreach (var segment in segments)
        {
            if (builder.Length > 0)
            {
                builder.Append('.');
            }

            var sanitized = new string(
                segment
                    .Select(character =>
                        char.IsLetterOrDigit(character) || character == '_'
                            ? character
                            : '_')
                    .ToArray());

            if (sanitized.Length == 0 || char.IsDigit(sanitized[0]))
            {
                builder.Append('_');
            }

            builder.Append(sanitized);
        }

        return builder.Length == 0 ? "_" : builder.ToString();
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex IdentifierRegex();
}
