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
        TextReader input,
        TextWriter output,
        TextWriter error)
    {
        try
        {
            var interaction = parseResult.GetValue(arguments.Interactive)
                ? new ConsoleInteraction(input, output)
                : null;

            var name = ResolveName(parseResult, arguments, interaction);
            var outputDirectory = Path.GetFullPath(
                parseResult.GetValue(arguments.Output) ?? ".");

            var baseNamespace = ResolveNamespace(
                parseResult,
                arguments,
                interaction,
                outputDirectory);

            var plan = GenerationPlanner.Create(
                kind,
                name,
                baseNamespace,
                outputDirectory,
                parseResult.GetValue(arguments.Flat),
                TemplateCatalog.Discover(outputDirectory));

            return new GenerationWriter(output, error, outputDirectory, interaction).Write(
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

    private static string ResolveName(
        ParseResult parseResult,
        GeneratorArguments arguments,
        ConsoleInteraction? interaction)
    {
        var rawName = parseResult.GetValue(arguments.Name);

        return interaction is not null && string.IsNullOrWhiteSpace(rawName)
            ? interaction.AskUntil("Name: ", NameNormalizer.Normalize)
            : NameNormalizer.Normalize(rawName ?? string.Empty);
    }

    private static string ResolveNamespace(
        ParseResult parseResult,
        GeneratorArguments arguments,
        ConsoleInteraction? interaction,
        string outputDirectory)
    {
        try
        {
            return NamespaceResolver.Resolve(
                outputDirectory,
                parseResult.GetValue(arguments.Namespace));
        }
        catch (CliException) when (interaction is not null)
        {
            return interaction.AskUntil("Namespace: ", value =>
            {
                NamespaceResolver.Validate(value);

                return value;
            });
        }
    }
}

/// <summary>
/// Holds the command-line symbols used by a generator command.
/// </summary>
internal sealed record GeneratorArguments(
    Argument<string?> Name,
    Option<string> Output,
    Option<string?> Namespace,
    Option<bool> Force,
    Option<bool> DryRun,
    Option<bool> Flat,
    Option<bool> Interactive);
