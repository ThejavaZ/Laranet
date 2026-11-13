using System.CommandLine;
using Laranet.Cli.Generation;

namespace Laranet.Cli.Commands;

/// <summary>
/// Executes <c>make:crud</c> from parsed command-line input.
/// </summary>
internal static class CrudRunner
{
    public static int Run(
        ParseResult parseResult,
        CrudArguments arguments,
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

            var profile = ResolveProfile(parseResult, arguments, interaction);
            var fields = CrudFieldParser.Parse(parseResult.GetValue(arguments.Fields));
            var dbContextType = ResolveContext(
                parseResult,
                arguments,
                interaction,
                outputDirectory);

            var context = new GenerationContext(
                name,
                baseNamespace,
                outputDirectory,
                parseResult.GetValue(arguments.Flat),
                TemplateCatalog.Discover(outputDirectory));

            var plan = new CrudGenerator(profile, fields, dbContextType).Create(context);

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
        CrudArguments arguments,
        ConsoleInteraction? interaction)
    {
        var rawName = parseResult.GetValue(arguments.Name);

        return interaction is not null && string.IsNullOrWhiteSpace(rawName)
            ? interaction.AskUntil("Name: ", NameNormalizer.Normalize)
            : NameNormalizer.Normalize(rawName ?? string.Empty);
    }

    private static string ResolveNamespace(
        ParseResult parseResult,
        CrudArguments arguments,
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

    private static CrudProfile ResolveProfile(
        ParseResult parseResult,
        CrudArguments arguments,
        ConsoleInteraction? interaction)
    {
        var value = parseResult.GetValue(arguments.Profile);

        return interaction is not null && string.IsNullOrWhiteSpace(value)
            ? interaction.AskCrudProfile()
            : CrudProfileParser.Parse(value);
    }

    private static string ResolveContext(
        ParseResult parseResult,
        CrudArguments arguments,
        ConsoleInteraction? interaction,
        string outputDirectory)
    {
        var explicitContext = parseResult.GetValue(arguments.Context);

        try
        {
            return DbContextResolver.Resolve(outputDirectory, explicitContext);
        }
        catch (CliException) when (
            interaction is not null && string.IsNullOrWhiteSpace(explicitContext))
        {
            return interaction.AskUntil("DbContext: ", value =>
            {
                DbContextResolver.Validate(value);

                return value.Trim();
            });
        }
    }
}
