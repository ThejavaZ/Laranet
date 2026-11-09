using System.CommandLine;
using Laranet.Cli.Generation;

namespace Laranet.Cli.Commands;

/// <summary>
/// Executes the project creation command from parsed command-line input.
/// </summary>
internal static class NewProjectRunner
{
    public static int Run(
        ParseResult parseResult,
        NewProjectArguments arguments,
        TextWriter output,
        TextWriter error)
    {
        try
        {
            var projectName = ProjectName.Folder(
                parseResult.GetValue(arguments.Name) ?? string.Empty);
            var outputRoot = Path.GetFullPath(
                parseResult.GetValue(arguments.Output) ?? ".");
            var projectDirectory = Path.Combine(outputRoot, projectName);
            var force = parseResult.GetValue(arguments.Force);
            var dryRun = parseResult.GetValue(arguments.DryRun);

            if (!force
                && Directory.Exists(projectDirectory)
                && Directory.EnumerateFileSystemEntries(projectDirectory).Any())
            {
                error.WriteLine(
                    $"error: {projectName} already exists and is not empty. Use --force to write into it anyway.");

                return 1;
            }

            var explicitNamespace = parseResult.GetValue(arguments.Namespace);
            var rootNamespace = string.IsNullOrWhiteSpace(explicitNamespace)
                ? ProjectName.Namespace(projectName)
                : explicitNamespace.Trim();

            NamespaceResolver.Validate(rootNamespace);

            var packages = new List<string>();

            if (parseResult.GetValue(arguments.WithData))
            {
                packages.Add("Laranet.Data");
            }

            if (parseResult.GetValue(arguments.WithEfCore))
            {
                packages.Add("Laranet.EntityFrameworkCore");
            }

            if (parseResult.GetValue(arguments.WithAspNetCore))
            {
                packages.Add("Laranet.AspNetCore");
            }

            var plan = ProjectPlanner.Create(
                projectName,
                rootNamespace,
                projectDirectory,
                ProjectTemplates.Parse(parseResult.GetValue(arguments.Template)),
                packages);

            var exitCode = new GenerationWriter(output, error, outputRoot).Write(
                plan,
                force,
                dryRun);

            if (exitCode == 0 && !dryRun)
            {
                output.WriteLine();
                output.WriteLine("Next steps:");
                output.WriteLine($"  cd \"{projectDirectory}\"");
                output.WriteLine("  dotnet run");
            }

            return exitCode;
        }
        catch (CliException exception)
        {
            error.WriteLine($"error: {exception.Message}");

            return 1;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            error.WriteLine($"error: {exception.Message}");

            return 1;
        }
    }
}

/// <summary>
/// Holds the command-line symbols used by the project creation command.
/// </summary>
internal sealed record NewProjectArguments(
    Argument<string> Name,
    Option<string> Output,
    Option<string?> Namespace,
    Option<string?> Template,
    Option<bool> WithData,
    Option<bool> WithEfCore,
    Option<bool> WithAspNetCore,
    Option<bool> Force,
    Option<bool> DryRun);
