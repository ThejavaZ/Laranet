using System.CommandLine;
using System.Text.Json;
using Laranet.Cli.Generation;

namespace Laranet.Cli.Commands;

/// <summary>
/// Creates the admin installation check command.
/// </summary>
internal static class AdminCheckCommand
{
    public static Command Create(
        TextWriter output,
        TextWriter error)
    {
        var pathOption = new Option<string>("--path", "-p")
        {
            Description = "The project directory to inspect. Defaults to the current directory.",
            DefaultValueFactory = _ => ".",
        };
        var jsonOption = new Option<bool>("--json")
        {
            Description = "Writes the report as JSON.",
        };

        var command = new Command(
            "admin:check",
            "Validates the Laranet admin installation.");

        command.Options.Add(pathOption);
        command.Options.Add(jsonOption);

        command.SetAction(
            parseResult => AdminCheckRunner.Run(
                parseResult,
                pathOption,
                jsonOption,
                output,
                error));

        return command;
    }
}

/// <summary>
/// Executes <c>admin:check</c> from parsed command-line input.
/// </summary>
internal static class AdminCheckRunner
{
    public static int Run(
        ParseResult parseResult,
        Option<string> pathOption,
        Option<bool> jsonOption,
        TextWriter output,
        TextWriter error)
    {
        try
        {
            var rootPath = Path.GetFullPath(parseResult.GetValue(pathOption) ?? ".");

            if (File.Exists(rootPath))
            {
                rootPath = Path.GetDirectoryName(rootPath)!;
            }

            if (!Directory.Exists(rootPath))
            {
                error.WriteLine($"error: directory '{rootPath}' does not exist.");
                return 2;
            }

            var json = parseResult.GetValue(jsonOption);
            var diagnostics = new List<AdminCheckDiagnostic>();

            // 1. Check admin package
            var projectFile = AdminPackageValidator.FindProjectFile(rootPath);
            if (projectFile is null)
            {
                diagnostics.Add(new AdminCheckDiagnostic(
                    "ADMIN001",
                    "error",
                    "No .csproj file found. Run this command inside a .NET project."));
            }
            else
            {
                if (!AdminPackageValidator.HasPackageReference(projectFile, "Laranet.Admin.AspNetCore"))
                {
                    diagnostics.Add(new AdminCheckDiagnostic(
                        "ADMIN002",
                        "error",
                        "Laranet.Admin.AspNetCore is not referenced. Run: dotnet add package Laranet.Admin.AspNetCore",
                        projectFile));
                }

                // Scan all source files for registration patterns
                var allSource = string.Join(
                    "\n",
                    Directory.EnumerateFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                        .Where(path => !path.Split(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar).Any(segment => segment is "bin" or "obj"))
                        .Select(File.ReadAllText));

                // 2. Check authentication
                if (!allSource.Contains("AddDefaultIdentity", StringComparison.Ordinal)
                    && !allSource.Contains("AddIdentity", StringComparison.Ordinal)
                    && !allSource.Contains("AddLaranetAdminAuthentication", StringComparison.Ordinal)
                    && !allSource.Contains("AddAuthentication", StringComparison.Ordinal))
                {
                    diagnostics.Add(new AdminCheckDiagnostic(
                        "ADMIN003",
                        "warning",
                        "No authentication is configured. AddLaranetAdminAuthentication or another ASP.NET Core authentication scheme.",
                        projectFile));
                }

                // 3. Check Razor Pages
                if (!allSource.Contains("AddRazorPages", StringComparison.Ordinal))
                {
                    diagnostics.Add(new AdminCheckDiagnostic(
                        "ADMIN004",
                        "error",
                        "Razor Pages are not registered. Add builder.Services.AddRazorPages().",
                        projectFile));
                }

                if (!allSource.Contains("MapRazorPages", StringComparison.Ordinal))
                {
                    diagnostics.Add(new AdminCheckDiagnostic(
                        "ADMIN005",
                        "error",
                        "Razor Pages are not mapped. Add app.MapRazorPages().",
                        projectFile));
                }
            }

            // 4. Check for admin resources
            var resourceFiles = Directory
                .EnumerateFiles(rootPath, "*Resource.cs", SearchOption.AllDirectories)
                .Where(path => !path.Split(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar).Any(segment => segment is "bin" or "obj"))
                .ToList();

            var adminResources = resourceFiles
                .Where(path => File.ReadAllText(path).Contains("AdminResource<", StringComparison.Ordinal))
                .ToList();

            if (adminResources.Count == 0)
            {
                diagnostics.Add(new AdminCheckDiagnostic(
                    "ADMIN006",
                    "error",
                    "No admin resources found. Run `laranet admin:resource <Entity>` to create one."));
            }

            // 5. Check for authorization policies
            var hasAuthorization = projectFile is not null
                && File.ReadAllText(projectFile).Contains("AddAuthorization", StringComparison.Ordinal);

            if (!hasAuthorization && adminResources.Count > 0)
            {
                diagnostics.Add(new AdminCheckDiagnostic(
                    "ADMIN007",
                    "warning",
                    "No authorization policies found. Resources may be accessible without authentication.",
                    projectFile));
            }

            // 6. Check for tenant filtering
            var hasTenantFilter = Directory
                .EnumerateFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Split(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar).Any(segment => segment is "bin" or "obj"))
                .Any(path => File.ReadAllText(path).Contains("CompanyId", StringComparison.Ordinal));

            if (!hasTenantFilter && adminResources.Count > 0)
            {
                diagnostics.Add(new AdminCheckDiagnostic(
                    "ADMIN008",
                    "warning",
                    "No tenant filtering detected. If your app is multi-tenant, ensure filters are applied.",
                    projectFile));
            }

            var errors = diagnostics.Count(d => d.Severity == "error");
            var warnings = diagnostics.Count - errors;
            var exitCode = errors > 0 ? 1 : 0;

            var report = new AdminCheckReport(diagnostics, errors, warnings, exitCode);

            if (json)
            {
                output.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }));
            }
            else
            {
                if (diagnostics.Count == 0)
                {
                    CliStyle.WriteSuccess(output, "Admin installation looks good.");
                }
                else
                {
                    foreach (var diagnostic in diagnostics)
                    {
                        var severity = diagnostic.Severity == "error" ? "error" : "warning";
                        var location = diagnostic.Path is null
                            ? string.Empty
                            : $" ({Path.GetRelativePath(rootPath, diagnostic.Path)})";

                        if (severity == "error")
                        {
                            CliStyle.WriteError(output, $"{diagnostic.Code}{location}: {diagnostic.Message}");
                        }
                        else
                        {
                            CliStyle.WriteWarning(output, $"{diagnostic.Code}{location}: {diagnostic.Message}");
                        }
                    }

                    output.WriteLine();
                    output.WriteLine($"Found {errors} error(s) and {warnings} warning(s).");
                }
            }

            return exitCode;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            error.WriteLine($"error: {exception.Message}");
            return 2;
        }
    }
}

internal sealed record AdminCheckDiagnostic(
    string Code,
    string Severity,
    string Message,
    string? Path = null);

internal sealed record AdminCheckReport(
    IReadOnlyList<AdminCheckDiagnostic> Diagnostics,
    int ErrorCount,
    int WarningCount,
    int ExitCode);
