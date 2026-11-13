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
        TextReader input,
        TextWriter output,
        TextWriter error)
    {
        try
        {
            var interaction = parseResult.GetValue(arguments.Interactive)
                ? new ConsoleInteraction(input, output)
                : null;

            var rawName = parseResult.GetValue(arguments.Name);
            var projectName = interaction is not null && string.IsNullOrWhiteSpace(rawName)
                ? interaction.AskUntil("Project name: ", ProjectName.Folder)
                : ProjectName.Folder(rawName ?? string.Empty);

            var templateValue = parseResult.GetValue(arguments.Template);
            var template = interaction is not null && string.IsNullOrWhiteSpace(templateValue)
                ? interaction.AskTemplate()
                : ProjectTemplates.Parse(templateValue);

            var layoutValue = parseResult.GetValue(arguments.Layout);
            var layout = interaction is not null && string.IsNullOrWhiteSpace(layoutValue)
                ? interaction.AskLayout()
                : ProjectLayoutParser.Parse(layoutValue);

            var withData = parseResult.GetValue(arguments.WithData);
            var withEfCore = parseResult.GetValue(arguments.WithEfCore);
            var withAspNetCore = parseResult.GetValue(arguments.WithAspNetCore);

            if (interaction is not null && !withData && !withEfCore && !withAspNetCore)
            {
                withData = interaction.Confirm("Reference Laranet.Data? [y/N] ");
                withEfCore = interaction.Confirm("Reference Laranet.EntityFrameworkCore? [y/N] ");
                withAspNetCore = interaction.Confirm("Reference Laranet.AspNetCore? [y/N] ");
            }

            var outputRoot = Path.GetFullPath(
                parseResult.GetValue(arguments.Output) ?? ".");
            var projectDirectory = Path.Combine(outputRoot, projectName);
            var force = parseResult.GetValue(arguments.Force);
            var dryRun = parseResult.GetValue(arguments.DryRun);

            if (!force
                && Directory.Exists(projectDirectory)
                && Directory.EnumerateFileSystemEntries(projectDirectory).Any())
            {
                if (interaction is null)
                {
                    error.WriteLine(
                        $"error: {projectName} already exists and is not empty. Use --force to write into it anyway.");

                    return 1;
                }

                if (!interaction.Confirm(
                    $"{projectName} already exists and is not empty. Write into it anyway? [y/N] "))
                {
                    error.WriteLine("error: cancelled.");

                    return 1;
                }

                force = true;
            }

            var explicitNamespace = parseResult.GetValue(arguments.Namespace);
            var rootNamespace = string.IsNullOrWhiteSpace(explicitNamespace)
                ? ProjectName.Namespace(projectName)
                : explicitNamespace.Trim();

            NamespaceResolver.Validate(rootNamespace);

            var packages = new List<string>();

            if (withData)
            {
                packages.Add("Laranet.Data");
            }

            if (withEfCore)
            {
                packages.Add("Laranet.EntityFrameworkCore");
            }

            if (withAspNetCore)
            {
                packages.Add("Laranet.AspNetCore");
            }

            var plan = ProjectPlanner.Create(
                projectName,
                rootNamespace,
                projectDirectory,
                template,
                layout,
                parseResult.GetValue(arguments.Sln),
                packages);

            var exitCode = new GenerationWriter(output, error, outputRoot, interaction).Write(
                plan,
                new GenerationOptions(force, dryRun));

            if (exitCode == 0 && !dryRun)
            {
                output.WriteLine();
                output.WriteLine("Next steps:");
                output.WriteLine($"  cd \"{projectDirectory}\"");

                if (layout == ProjectLayout.Repository)
                {
                    output.WriteLine(
                        $"  dotnet run --project src/{projectName}/{projectName}.csproj");
                }
                else
                {
                    output.WriteLine("  dotnet run");
                }
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
    Argument<string?> Name,
    Option<string> Output,
    Option<string?> Namespace,
    Option<string?> Template,
    Option<string?> Layout,
    Option<bool> Sln,
    Option<bool> WithData,
    Option<bool> WithEfCore,
    Option<bool> WithAspNetCore,
    Option<bool> Force,
    Option<bool> DryRun,
    Option<bool> Interactive);
