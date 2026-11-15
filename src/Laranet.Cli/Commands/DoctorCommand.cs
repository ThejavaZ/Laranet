using System.CommandLine;
using System.Text.Json;
using Laranet.Cli.Doctor;

namespace Laranet.Cli.Commands;

/// <summary>
/// Creates the deterministic project diagnostics command.
/// </summary>
internal static class DoctorCommand
{
    public static Command Create(TextWriter output, TextWriter error)
    {
        var path = new Option<string>("--path", "-p")
        {
            Description = "The project or directory to inspect. Defaults to the current directory.",
            DefaultValueFactory = _ => ".",
        };
        var json = new Option<bool>("--json")
        {
            Description = "Writes the report as JSON.",
        };
        var strict = new Option<bool>("--strict")
        {
            Description = "Returns exit code 1 when warnings are found.",
        };
        var pagination = new Option<bool>("--check-pagination")
        {
            Description = "Enables the heuristic for unbounded endpoint queries.",
        };
        var controllers = new Option<bool>("--check-controller-dbcontext")
        {
            Description = "Enables the rule that checks DbContext usage in controllers.",
        };
        var dtoEntities = new Option<bool>("--check-dto-entities")
        {
            Description = "Enables the heuristic for DTOs exposing entity types.",
        };
        var command = new Command(
            "doctor",
            "Checks a project for deterministic dependency, version, and code hazards.");

        command.Options.Add(path);
        command.Options.Add(json);
        command.Options.Add(strict);
        command.Options.Add(pagination);
        command.Options.Add(controllers);
        command.Options.Add(dtoEntities);
        command.SetAction(
            parseResult =>
            {
                try
                {
                    var rootPath = Path.GetFullPath(parseResult.GetValue(path) ?? ".");

                    if (File.Exists(rootPath))
                    {
                        rootPath = Path.GetDirectoryName(rootPath)!;
                    }

                    if (!Directory.Exists(rootPath))
                    {
                        error.WriteLine($"error: directory '{rootPath}' does not exist.");

                        return 2;
                    }

                    var options = new DoctorOptions(
                        rootPath,
                        parseResult.GetValue(json),
                        parseResult.GetValue(strict),
                        parseResult.GetValue(pagination),
                        parseResult.GetValue(controllers),
                        parseResult.GetValue(dtoEntities));
                    var report = new DoctorAnalyzer().Analyze(options);

                    WriteReport(report, options.Json, rootPath, output);

                    return report.ExitCode;
                }
                catch (Exception exception) when (
                    exception is IOException
                        or UnauthorizedAccessException
                        or System.Xml.XmlException)
                {
                    error.WriteLine($"error: {exception.Message}");

                    return 2;
                }
            });

        return command;
    }

    private static void WriteReport(
        DoctorReport report,
        bool json,
        string rootPath,
        TextWriter output)
    {
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(report, DoctorJson.Options));

            return;
        }

        if (report.Diagnostics.Count == 0)
        {
            output.WriteLine($"No diagnostics found in {rootPath}.");

            return;
        }

        foreach (var diagnostic in report.Diagnostics)
        {
            var severity = diagnostic.Severity.ToString().ToLowerInvariant();
            var location = diagnostic.Path is null
                ? string.Empty
                : $" {Path.GetRelativePath(rootPath, diagnostic.Path)}" +
                  (diagnostic.Line is null ? string.Empty : $":{diagnostic.Line}");

            output.WriteLine(
                $"{severity} {diagnostic.Code}{location}: {diagnostic.Message}");
        }

        output.WriteLine(
            $"Found {report.ErrorCount} error(s) and {report.WarningCount} warning(s).");
    }
}
