using System.Xml.Linq;

namespace Laranet.Cli.Generation;

/// <summary>
/// Validates that the current project references the required Laranet packages.
/// </summary>
internal static class AdminPackageValidator
{
    /// <summary>
    /// Checks if the current project references Laranet.Admin.AspNetCore.
    /// </summary>
    /// <param name="outputDirectory">The directory to search from.</param>
    /// <returns>The path to the project file if found.</returns>
    /// <exception cref="CliException">If the package is not found or no project exists.</exception>
    public static string ValidateAdminPackage(string outputDirectory)
    {
        var projectFile = FindProjectFile(outputDirectory);

        if (projectFile is null)
        {
            throw new CliException(
                "No .csproj file found. Run this command inside a .NET project directory.");
        }

        // Check PackageReference in the project
        if (HasPackageReference(projectFile, "Laranet.Admin.AspNetCore"))
        {
            return projectFile;
        }

        // Plan B: check obj/project.assets.json
        var assetsPath = Path.Combine(
            Path.GetDirectoryName(projectFile)!,
            "obj",
            "project.assets.json");

        if (File.Exists(assetsPath))
        {
            var assets = File.ReadAllText(assetsPath);

            if (assets.Contains("Laranet.Admin.AspNetCore", StringComparison.Ordinal))
            {
                return projectFile;
            }
        }

        throw new CliException(
            "The Laranet.Admin.AspNetCore package is not referenced. " +
            "Run: dotnet add package Laranet.Admin.AspNetCore");
    }

    public static string? FindProjectFile(string outputDirectory)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(outputDirectory));

        while (directory is not null)
        {
            var projectFile = directory
                .EnumerateFiles("*.csproj")
                .OrderBy(file => file.Name, StringComparer.Ordinal)
                .FirstOrDefault();

            if (projectFile is not null)
            {
                return projectFile.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    public static bool HasPackageReference(string projectFile, string packageName)
    {
        try
        {
            var document = XDocument.Load(projectFile);

            return document
                .Descendants()
                .Any(element =>
                    element.Name.LocalName == "PackageReference"
                    && string.Equals(
                        (string?)element.Attribute("Include"),
                        packageName,
                        StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}
