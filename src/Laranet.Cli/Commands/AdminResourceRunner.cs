using System.CommandLine;
using Laranet.Cli.Generation;

namespace Laranet.Cli.Commands;

/// <summary>
/// Executes <c>make:admin-resource</c> from parsed command-line input.
/// </summary>
internal static class AdminResourceRunner
{
    public static int Run(
        ParseResult parseResult,
        AdminResourceArguments arguments,
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

            var fields = CrudFieldParser.Parse(parseResult.GetValue(arguments.Fields), allowId: true);
            var policy = parseResult.GetValue(arguments.Policy);

            var context = new GenerationContext(
                name,
                baseNamespace,
                outputDirectory,
                parseResult.GetValue(arguments.Flat),
                TemplateCatalog.Discover(outputDirectory),
                new GenerationSettings(Fields: fields));

            var plan = new AdminResourceGenerator(policy).Create(context);

            return new GenerationWriter(output, error, outputDirectory, interaction).Write(
                plan,
                new GenerationOptions(
                    parseResult.GetValue(arguments.Force),
                    parseResult.GetValue(arguments.DryRun)));
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
        AdminResourceArguments arguments,
        ConsoleInteraction? interaction)
    {
        var rawName = parseResult.GetValue(arguments.Name);

        return interaction is not null && string.IsNullOrWhiteSpace(rawName)
            ? interaction.AskUntil("Name: ", NameNormalizer.Normalize)
            : NameNormalizer.Normalize(rawName ?? string.Empty);
    }

    private static string ResolveNamespace(
        ParseResult parseResult,
        AdminResourceArguments arguments,
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
