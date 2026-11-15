using System.CommandLine;
using Laranet.Cli.Generation;

namespace Laranet.Cli.Commands;

/// <summary>
/// Creates the admin resource generation command.
/// </summary>
internal static class AdminResourceCommand
{
    public static Command Create(
        TextWriter output,
        TextWriter error,
        TextReader input)
    {
        var arguments = new AdminResourceArguments(
            new Argument<string?>("name")
            {
                Description = "The entity type to generate an admin resource for.",
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
            new Option<string?>("--fields")
            {
                Description = "The resource fields as Name:type pairs separated by commas.",
            },
            new Option<string?>("--policy")
            {
                Description = "The authorization policy name. Defaults to Admin.<Entity>.",
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

        var command = new Command(
            "make:admin-resource",
            "Generates a Laranet.Admin resource class for an entity.");

        command.Arguments.Add(arguments.Name);
        command.Options.Add(arguments.Output);
        command.Options.Add(arguments.Namespace);
        command.Options.Add(arguments.Fields);
        command.Options.Add(arguments.Policy);
        command.Options.Add(arguments.Force);
        command.Options.Add(arguments.DryRun);
        command.Options.Add(arguments.Flat);
        command.Options.Add(arguments.Interactive);

        command.SetAction(
            parseResult => AdminResourceRunner.Run(
                parseResult,
                arguments,
                input,
                output,
                error));

        return command;
    }
}

/// <summary>
/// Holds the command-line symbols used by <c>make:admin-resource</c>.
/// </summary>
internal sealed record AdminResourceArguments(
    Argument<string?> Name,
    Option<string> Output,
    Option<string?> Namespace,
    Option<string?> Fields,
    Option<string?> Policy,
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
