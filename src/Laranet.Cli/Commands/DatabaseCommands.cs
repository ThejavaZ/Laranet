using System.CommandLine;
using Laranet.Cli.Generation;

namespace Laranet.Cli.Commands;

/// <summary>
/// Creates the database and configuration generation commands.
/// </summary>
internal static class DatabaseCommands
{
    public static IEnumerable<Command> Create(
        TextWriter output,
        TextWriter error,
        TextReader input)
    {
        yield return CreateDbContext(output, error, input);
        yield return CreateConfig(output, error, input);
    }

    private static Command CreateDbContext(
        TextWriter output,
        TextWriter error,
        TextReader input)
    {
        var arguments = new DbContextArguments(
            CreateNameArgument("The DbContext name."),
            CreateOutputOption(),
            CreateNamespaceOption(),
            new Option<string>("--provider")
            {
                Description = "The provider: sqlserver (default), postgres, mysql, or sqlite.",
                DefaultValueFactory = _ => "sqlserver",
            },
            new Option<string?>("--entities")
            {
                Description = "Comma-separated entity type names.",
            },
            CreateForceOption(),
            CreateDryRunOption(),
            CreateFlatOption(),
            CreateInteractiveOption());
        var command = CreateCommand(
            "make:dbcontext",
            "Generates an EF Core DbContext, configurations, and design-time factory.",
            arguments);

        command.SetAction(
            parseResult => GeneratorRunner.Run(
                GeneratorKind.DbContext,
                parseResult,
                arguments,
                input,
                output,
                error,
                parse => new GenerationSettings(
                    Provider: ParseProvider(parse.GetValue(arguments.Provider)),
                    Entities: ParseEntities(parse.GetValue(arguments.Entities)))));

        return command;
    }

    private static Command CreateConfig(
        TextWriter output,
        TextWriter error,
        TextReader input)
    {
        var arguments = new ConfigArguments(
            CreateNameArgument("The options name."),
            CreateOutputOption(),
            CreateNamespaceOption(),
            new Option<string[]>("--section")
            {
                Description = "A configuration section key. Repeat for named option registrations.",
                Arity = ArgumentArity.ZeroOrMore,
            },
            CreateForceOption(),
            CreateDryRunOption(),
            CreateFlatOption(),
            CreateInteractiveOption());
        var command = CreateCommand(
            "make:config",
            "Generates native options and configuration registration code.",
            arguments);

        command.SetAction(
            parseResult => GeneratorRunner.Run(
                GeneratorKind.Config,
                parseResult,
                arguments,
                input,
                output,
                error,
                parse => new GenerationSettings(
                    Sections: parse.GetValue(arguments.Sections) ?? [])));

        return command;
    }

    private static Command CreateCommand(
        string name,
        string description,
        GeneratorArguments arguments)
    {
        var command = new Command(name, description);

        command.Arguments.Add(arguments.Name);
        command.Options.Add(arguments.Output);
        command.Options.Add(arguments.Namespace);
        command.Options.Add(arguments.Force);
        command.Options.Add(arguments.DryRun);
        command.Options.Add(arguments.Flat);
        command.Options.Add(arguments.Interactive);

        if (arguments is DbContextArguments dbContext)
        {
            command.Options.Add(dbContext.Provider);
            command.Options.Add(dbContext.Entities);
        }
        else if (arguments is ConfigArguments config)
        {
            command.Options.Add(config.Sections);
        }

        return command;
    }

    private static DatabaseProvider ParseProvider(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "sqlserver" => DatabaseProvider.SqlServer,
            "postgres" => DatabaseProvider.Postgres,
            "mysql" => DatabaseProvider.MySql,
            "sqlite" => DatabaseProvider.Sqlite,
            _ => throw new CliException(
                "The provider must be one of: sqlserver, postgres, mysql, sqlite."),
        };
    }

    private static IReadOnlyList<string> ParseEntities(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NameNormalizer.Normalize)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static Argument<string?> CreateNameArgument(string description)
    {
        return new Argument<string?>("name")
        {
            Description = description,
            Arity = ArgumentArity.ZeroOrOne,
        };
    }

    private static Option<string> CreateOutputOption() => new("--output", "-o")
    {
        Description = "The output directory. Defaults to the current directory.",
        DefaultValueFactory = _ => ".",
    };

    private static Option<string?> CreateNamespaceOption() => new("--namespace", "-n")
    {
        Description = "The namespace for generated code. Inferred from the nearest project by default.",
    };

    private static Option<bool> CreateForceOption() => new("--force", "-f")
    {
        Description = "Overwrites existing files.",
    };

    private static Option<bool> CreateDryRunOption() => new("--dry-run")
    {
        Description = "Shows the files that would be generated without writing them.",
    };

    private static Option<bool> CreateFlatOption() => new("--flat")
    {
        Description = "Places files directly in the output directory.",
    };

    private static Option<bool> CreateInteractiveOption() => new("--interactive", "-i")
    {
        Description = "Prompts for missing values and overwrite confirmation.",
    };
}

internal sealed record DbContextArguments(
    Argument<string?> Name,
    Option<string> Output,
    Option<string?> Namespace,
    Option<string> Provider,
    Option<string?> Entities,
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

internal sealed record ConfigArguments(
    Argument<string?> Name,
    Option<string> Output,
    Option<string?> Namespace,
    Option<string[]> Sections,
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
