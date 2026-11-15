using System.CommandLine;
using Laranet.Cli.Generation;

namespace Laranet.Cli.Commands;

/// <summary>
/// Creates the CQRS code generation commands.
/// </summary>
internal static class CqrsCommands
{
    public static IEnumerable<Command> Create(
        TextWriter output,
        TextWriter error,
        TextReader input)
    {
        yield return CreateRequest(
            "make:command",
            "Generates a command record and, optionally, its handler.",
            GeneratorKind.Command,
            output,
            error,
            input);

        yield return CreateRequest(
            "make:query",
            "Generates a query record and, optionally, its handler.",
            GeneratorKind.Query,
            output,
            error,
            input);

        yield return CreateHandler(output, error, input);
    }

    private static Command CreateRequest(
        string name,
        string description,
        GeneratorKind kind,
        TextWriter output,
        TextWriter error,
        TextReader input)
    {
        var arguments = CreateArguments(includeHandler: true);
        var command = CreateCommand(name, description, arguments);

        command.Options.Add(arguments.WithHandler!);
        SetAction(command, kind, arguments, output, error, input);

        return command;
    }

    private static Command CreateHandler(
        TextWriter output,
        TextWriter error,
        TextReader input)
    {
        var arguments = CreateArguments(includeHandler: false);
        var command = CreateCommand(
            "make:handler",
            "Generates a handler for a command or query.",
            arguments);

        SetAction(command, GeneratorKind.Handler, arguments, output, error, input);

        return command;
    }

    private static CqrsArguments CreateArguments(bool includeHandler)
    {
        return new CqrsArguments(
            new Argument<string?>("name")
            {
                Description = includeHandler
                    ? "The command or query name."
                    : "The command or query type name, including Command or Query.",
                Arity = ArgumentArity.ZeroOrOne,
            },
            new Option<string>("--output", "-o")
            {
                Description = "The output directory. Defaults to the current directory.",
                DefaultValueFactory = _ => ".",
            },
            new Option<string?>("--namespace", "-n")
            {
                Description = "The namespace for generated code. Inferred from the nearest project by default.",
            },
            new Option<string>("--mediator")
            {
                Description = "The contract style: none (default), mediatr, or wolverine.",
                DefaultValueFactory = _ => "none",
            },
            new Option<string>("--profile")
            {
                Description = "The organization profile: simple (default), standard, or clean.",
                DefaultValueFactory = _ => "simple",
            },
            includeHandler
                ? new Option<bool>("--with-handler")
                {
                    Description = "Generates the corresponding handler too.",
                }
                : null,
            new Option<bool>("--with-tests")
            {
                Description = "Generates an xUnit test scaffold for the request.",
            },
            new Option<bool>("--force", "-f")
            {
                Description = "Overwrites existing files.",
            },
            new Option<bool>("--dry-run")
            {
                Description = "Shows the files that would be generated without writing them.",
            },
            new Option<bool>("--flat")
            {
                Description = "Places files directly in the output directory.",
            },
            new Option<bool>("--interactive", "-i")
            {
                Description = "Prompts for missing values and overwrite confirmation.",
            });
    }

    private static Command CreateCommand(
        string name,
        string description,
        CqrsArguments arguments)
    {
        var command = new Command(name, description);

        command.Arguments.Add(arguments.Name);
        command.Options.Add(arguments.Output);
        command.Options.Add(arguments.Namespace);
        command.Options.Add(arguments.Mediator);
        command.Options.Add(arguments.Profile);
        command.Options.Add(arguments.WithTests);
        command.Options.Add(arguments.Force);
        command.Options.Add(arguments.DryRun);
        command.Options.Add(arguments.Flat);
        command.Options.Add(arguments.Interactive);

        return command;
    }

    private static void SetAction(
        Command command,
        GeneratorKind kind,
        CqrsArguments arguments,
        TextWriter output,
        TextWriter error,
        TextReader input)
    {
        command.SetAction(
            parseResult => GeneratorRunner.Run(
                kind,
                parseResult,
                arguments,
                input,
                output,
                error,
                parse => CreateSettings(parse, arguments)));
    }

    private static GenerationSettings CreateSettings(
        ParseResult parseResult,
        CqrsArguments arguments)
    {
        return new GenerationSettings(
            ParseMediator(parseResult.GetValue(arguments.Mediator)),
            ParseProfile(parseResult.GetValue(arguments.Profile)),
            arguments.WithHandler is not null &&
                parseResult.GetValue(arguments.WithHandler),
            parseResult.GetValue(arguments.WithTests));
    }

    private static MediatorKind ParseMediator(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "none" => MediatorKind.None,
            "mediatr" => MediatorKind.MediatR,
            "wolverine" => MediatorKind.Wolverine,
            _ => throw new CliException(
                "The mediator must be one of: none, mediatr, wolverine."),
        };
    }

    private static CqrsProfile ParseProfile(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "simple" => CqrsProfile.Simple,
            "standard" => CqrsProfile.Standard,
            "clean" => CqrsProfile.Clean,
            _ => throw new CliException(
                "The profile must be one of: simple, standard, clean."),
        };
    }
}

/// <summary>
/// Holds the command-line symbols used by CQRS generators.
/// </summary>
internal sealed record CqrsArguments(
    Argument<string?> Name,
    Option<string> Output,
    Option<string?> Namespace,
    Option<string> Mediator,
    Option<string> Profile,
    Option<bool>? WithHandler,
    Option<bool> WithTests,
    Option<bool> Force,
    Option<bool> DryRun,
    Option<bool> Flat,
    Option<bool> Interactive) : GeneratorArguments(
        Name,
        Output,
        Namespace,
        Force,
        DryRun,
        Flat,
        Interactive);
