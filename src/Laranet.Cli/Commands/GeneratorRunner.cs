using System.CommandLine;
using Laranet.Cli.Generation;

namespace Laranet.Cli.Commands;

/// <summary>
/// Executes generator commands from parsed command-line input.
/// </summary>
internal static class GeneratorRunner
{
    public static int Run(
        GeneratorKind kind,
        ParseResult parseResult,
        GeneratorArguments arguments,
        TextWriter output,
        TextWriter error)
    {
        try
        {
            var rawName = parseResult.GetValue(arguments.Name) ?? string.Empty;
            var name = NameNormalizer.Normalize(rawName);
            var outputDirectory = Path.GetFullPath(
                parseResult.GetValue(arguments.Output) ?? ".");

            var baseNamespace = NamespaceResolver.Resolve(
                outputDirectory,
                parseResult.GetValue(arguments.Namespace));

            var plan = GenerationPlanner.Create(
                kind,
                name,
                baseNamespace,
                outputDirectory,
                parseResult.GetValue(arguments.Flat),
                TemplateCatalog.Discover(outputDirectory));

            return new GenerationWriter(output, error, outputDirectory).Write(
                plan,
                parseResult.GetValue(arguments.Force),
                parseResult.GetValue(arguments.DryRun));
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
/// Holds the command-line symbols used by a generator command.
/// </summary>
internal sealed record GeneratorArguments(
    Argument<string> Name,
    Option<string> Output,
    Option<string?> Namespace,
    Option<bool> Force,
    Option<bool> DryRun,
    Option<bool> Flat);
